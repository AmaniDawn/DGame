using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor.Authoring;
using Unity.Pipeline.Models;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Pipeline.Editor.Commands.Scenes
{
    /// <summary>
    /// Scene-management authoring commands (CLI-193): create / open / save scenes, inspect open
    /// scenes, control the active scene, snapshot a scene's hierarchy, and manage the build scene
    /// list. They build on the same foundation as the asset commands — paths go through the
    /// <see cref="ProjectPaths"/> sandbox and results come back as the canonical
    /// <see cref="AuthoringResult"/> envelope so an agent can chain follow-up calls.
    ///
    /// WHY the play-mode guard: <see cref="EditorSceneManager"/> mutations (new/open/save/setActive)
    /// are undefined and destructive while the editor is entering or in play mode — they can discard
    /// in-flight play-mode state or leave the scene manager inconsistent. Rather than risk corrupting
    /// state, every mutating command refuses up front with a clear, *recoverable* error (exit code
    /// <see cref="ExitCode.Failed"/>) without touching scene state. The caller can exit play mode and retry.
    /// </summary>
    static class SceneCommands
    {
        const string Group = "scene";
        const string EditorGroup = "editor";

        private const string SceneExtension = ".unity";

        // create_scene --template values. "empty" is the historical default (blank scene); "default"
        // seeds Unity's built-in "3D" new-scene contents (Main Camera + Directional Light).
        private const string TemplateEmpty = "empty";
        private const string TemplateDefault = "default";

        #region Create / Open / Save

        [CliGroupCommand(Group, "Create a new scene and save it to the given path under the authoring root.", Tags = new[] { "scenes" }, Effect = CommandEffect.Destructive)]
        internal static ExitCode Create(CommandContext cmd,
            [CliArg("path", "Scene path relative to the authoring root (default Assets/); the Assets/ prefix and the .unity extension are optional. e.g. Scenes/Level1", Required = true)] string path,
            [CliArg("additive", "Open the new scene additively alongside currently open scenes instead of replacing them.", DefaultValue = false)] bool additive = false,
            [CliArg("template", "Initial contents: 'empty' (default) for a blank scene, or 'default' to seed a Main Camera + Directional Light matching Unity's built-in 3D template.", DefaultValue = TemplateEmpty)] string template = TemplateEmpty)
        {
            if (RefuseInPlayMode(cmd, CommandNames.SceneCreate))
                return ExitCode.Failed;

            // Validate the template up front (before touching scene state) so an unknown value fails
            // recoverably with a clear, enumerated error rather than silently doing the wrong thing.
            if (!TryResolveSceneSetup(template, out var sceneSetup))
            {
                cmd.Log.Error($"Unknown template '{template}'. Valid values: '{TemplateEmpty}', '{TemplateDefault}'.");
                return ExitCode.InvalidUsage;
            }

            var normalized = ResolveScenePath(path);
            EnsureParentFolder(normalized);

            // NewSceneMode.Single replaces all open scenes; Additive keeps them. NewSceneSetup.EmptyScene
            // gives a blank scene (no default camera/light) — predictable for programmatic population;
            // NewSceneSetup.DefaultGameObjects gives exactly a Main Camera (tagged MainCamera) + a
            // Directional Light, identical to Unity's built-in "3D" new-scene template.
            var setup = additive ? NewSceneMode.Additive : NewSceneMode.Single;
            var scene = EditorSceneManager.NewScene(sceneSetup, setup);

            if (!EditorSceneManager.SaveScene(scene, normalized))
            {
                cmd.Log.Error($"Failed to save new scene to '{normalized}'.");
                return ExitCode.Failed;
            }

            AssetDatabase.Refresh();
            cmd.Result.Write(DescribeSceneAsset(normalized));
            return ExitCode.Success;
        }

        [CliGroupCommand(Group, "Open an existing scene from the given path.", Tags = new[] { "scenes" }, Effect = CommandEffect.Destructive)]
        internal static ExitCode Open(CommandContext cmd,
            [CliArg("path", "Scene path relative to the authoring root (default Assets/); the Assets/ prefix and the .unity extension are optional.", Required = true)] string path,
            [CliArg("additive", "Open additively alongside currently open scenes instead of replacing them.", DefaultValue = false)] bool additive = false)
        {
            if (RefuseInPlayMode(cmd, CommandNames.SceneOpen))
                return ExitCode.Failed;

            var normalized = ResolveScenePath(path);
            if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(normalized)))
            {
                cmd.Log.Error($"No scene asset at '{normalized}'.");
                return ExitCode.NotFound;
            }

            var mode = additive ? OpenSceneMode.Additive : OpenSceneMode.Single;
            var scene = EditorSceneManager.OpenScene(normalized, mode);
            if (!scene.IsValid())
            {
                cmd.Log.Error($"Failed to open scene '{normalized}'.");
                return ExitCode.Failed;
            }

            cmd.Result.Write(DescribeSceneAsset(normalized));
            return ExitCode.Success;
        }

        [CliGroupCommand(Group, "Save an open scene. Saves the active scene when no path is given.", Tags = new[] { "scenes" }, Effect = CommandEffect.Idempotent)]
        internal static ExitCode Save(CommandContext cmd,
            [CliArg("path", "Path of the open scene to save (authoring-root relative; Assets/ prefix and .unity optional). Omit to save the active scene.")] string path = null)
        {
            if (RefuseInPlayMode(cmd, CommandNames.SceneSave))
                return ExitCode.Failed;

            var scene = string.IsNullOrEmpty(path)
                ? EditorSceneManager.GetActiveScene()
                : FindOpenScene(ResolveScenePath(path));

            if (!scene.IsValid() || !scene.isLoaded)
            {
                cmd.Log.Error(string.IsNullOrEmpty(path)
                    ? "No valid active scene to save."
                    : $"Scene '{path}' is not open.");
                return ExitCode.NotFound;
            }

            // An untitled/never-saved scene has an empty path. SaveScene(scene) with no explicit path
            // would either trigger a modal "Save Scene" dialog (hangs a headless editor) or fail
            // non-deterministically. Refuse up front with a recoverable error so the caller picks a path.
            if (string.IsNullOrEmpty(scene.path))
            {
                cmd.Log.Error(
                    $"Scene '{scene.name}' has never been saved (no path). " +
                    $"Create or save it with an explicit path first ({CommandNames.SceneCreate} <path>) — no scene state was changed.");
                return ExitCode.Failed;
            }

            if (!EditorSceneManager.SaveScene(scene))
            {
                cmd.Log.Error($"Failed to save scene '{scene.name}'.");
                return ExitCode.Failed;
            }

            AssetDatabase.Refresh();
            cmd.Result.Write(DescribeSceneAsset(scene.path));
            return ExitCode.Success;
        }

        [CliGroupCommand(EditorGroup, "Save all open scenes that have unsaved changes.", Tags = new[] { "scenes" }, Effect = CommandEffect.Idempotent)]
        internal static ExitCode SaveAll(CommandContext cmd)
        {
            if (RefuseInPlayMode(cmd, CommandNames.EditorSaveAll))
                return ExitCode.Failed;

            // SaveOpenScenes saves every loaded, dirty scene. A dirty untitled scene (empty path) would
            // make it pop a modal "Save Scene" dialog, which hangs a headless editor. Fail fast with a
            // recoverable error before saving anything so the caller saves that scene with an explicit path.
            var dirty = OpenScenes().Where(s => s.isDirty).ToList();
            var untitled = dirty.Where(s => string.IsNullOrEmpty(s.path)).Select(s => s.name).ToList();
            if (untitled.Count > 0)
            {
                cmd.Log.Error(
                    $"Cannot save all: {untitled.Count} dirty scene(s) have never been saved ({string.Join(", ", untitled)}). " +
                    $"Save each with an explicit path first ({CommandNames.SceneCreate} <path>) — no scenes were saved.");
                return ExitCode.Failed;
            }

            // Report which scenes it touched.
            var dirtyBefore = dirty.Select(s => s.path).ToList();

            if (!EditorSceneManager.SaveOpenScenes() && dirtyBefore.Count > 0)
            {
                cmd.Log.Error("Failed to save one or more open scenes.");
                return ExitCode.Failed;
            }

            AssetDatabase.Refresh();
            foreach (var scenePath in dirtyBefore)
                cmd.Result.WriteRecord(new { path = scenePath });
            cmd.Log.Info($"Saved {dirtyBefore.Count} scene(s).");
            return ExitCode.Success;
        }

        #endregion

        #region Inspect / Active

        [CliGroupCommand(Group, "List all currently open scenes with their load/active/dirty state.", Tags = new[] { "scenes" }, Effect = CommandEffect.ReadOnly)]
        internal static ExitCode ListOpen(CommandContext cmd)
        {
            // Read-only; safe in play mode, so no guard here.
            var active = SceneManager.GetActiveScene();
            foreach (var s in OpenScenes())
            {
                cmd.Result.WriteRecord(new
                {
                    name = s.name,
                    path = s.path,
                    isLoaded = s.isLoaded,
                    isDirty = s.isDirty,
                    isActive = s.handle == active.handle,
                    rootCount = s.isLoaded ? s.rootCount : 0
                });
            }

            return ExitCode.Success;
        }

        [CliGroupCommand(Group, "Set which open scene is the active scene (new objects are created in the active scene).", Tags = new[] { "scenes" }, Effect = CommandEffect.Idempotent)]
        internal static ExitCode SetActive(CommandContext cmd,
            [CliArg("path", "Path of an already-open scene to make active (authoring-root relative; Assets/ prefix and .unity optional).", Required = true)] string path)
        {
            if (RefuseInPlayMode(cmd, CommandNames.SceneSetActive))
                return ExitCode.Failed;

            var normalized = ResolveScenePath(path);
            var scene = FindOpenScene(normalized);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                cmd.Log.Error($"Scene '{path}' is not open; open it with {CommandNames.SceneOpen} before making it active.");
                return ExitCode.NotFound;
            }

            if (!SceneManager.SetActiveScene(scene))
            {
                cmd.Log.Error($"Failed to set '{normalized}' as the active scene.");
                return ExitCode.Failed;
            }

            cmd.Result.Write(DescribeSceneAsset(scene.path));
            return ExitCode.Success;
        }

        [CliGroupCommand(Group, "Return the GameObject tree of an open scene (or the active scene). Each node carries instanceId + hierarchyPath usable by GameObject commands.", Tags = new[] { "scenes" }, Effect = CommandEffect.ReadOnly)]
        internal static ExitCode GetHierarchy(CommandContext cmd,
            [CliArg("path", "Path of the open scene to snapshot (authoring-root relative; Assets/ prefix and .unity optional). Omit for the active scene.")] string path = null)
        {
            // Read-only; safe in play mode.
            var scene = string.IsNullOrEmpty(path)
                ? SceneManager.GetActiveScene()
                : FindOpenScene(ResolveScenePath(path));

            if (!scene.IsValid() || !scene.isLoaded)
            {
                cmd.Log.Error(string.IsNullOrEmpty(path)
                    ? "No valid active scene to read."
                    : $"Scene '{path}' is not open.");
                return ExitCode.NotFound;
            }

            var active = SceneManager.GetActiveScene();
            var hierarchy = new SceneHierarchy
            {
                SceneName = scene.name,
                ScenePath = scene.path,
                IsDirty = scene.isDirty,
                IsActive = scene.handle == active.handle
            };

            foreach (var root in scene.GetRootGameObjects())
                hierarchy.Roots.Add(BuildNode(root, "/" + root.name));

            cmd.Result.Write(hierarchy);
            return ExitCode.Success;
        }

        #endregion

        #region Build settings

        [CliGroupCommand(Group, "Add a scene to the Build Settings scene list (idempotent). Optionally enable it.", Tags = new[] { "scenes", "build/settings" }, Effect = CommandEffect.Additive)]
        internal static ExitCode AddToBuild(CommandContext cmd,
            [CliArg("path", "Scene path to add (authoring-root relative; Assets/ prefix and .unity optional).", Required = true)] string path,
            [CliArg("enabled", "Whether the scene is enabled in the build list.", DefaultValue = true)] bool enabled = true)
        {
            if (RefuseInPlayMode(cmd, CommandNames.SceneAddToBuild))
                return ExitCode.Failed;

            var normalized = ResolveScenePath(path);
            if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(normalized)))
            {
                cmd.Log.Error($"No scene asset at '{normalized}'.");
                return ExitCode.NotFound;
            }

            var scenes = EditorBuildSettings.scenes.ToList();
            var existing = scenes.FindIndex(s => PathsEqual(s.path, normalized));
            if (existing >= 0)
            {
                // Idempotent: just reconcile the enabled flag.
                if (scenes[existing].enabled != enabled)
                    scenes[existing] = new EditorBuildSettingsScene(normalized, enabled);
            }
            else
            {
                scenes.Add(new EditorBuildSettingsScene(normalized, enabled));
            }

            EditorBuildSettings.scenes = scenes.ToArray();
            cmd.Result.Write(new { path = normalized, enabled, buildIndex = SceneUtility.GetBuildIndexByScenePath(normalized), count = scenes.Count });
            return ExitCode.Success;
        }

        [CliGroupCommand(Group, "Remove a scene from the Build Settings scene list (idempotent).", Tags = new[] { "scenes", "build/settings" }, Effect = CommandEffect.Destructive)]
        internal static ExitCode RemoveFromBuild(CommandContext cmd,
            [CliArg("path", "Scene path to remove (authoring-root relative; Assets/ prefix and .unity optional).", Required = true)] string path)
        {
            if (RefuseInPlayMode(cmd, CommandNames.SceneRemoveFromBuild))
                return ExitCode.Failed;

            var normalized = ResolveScenePath(path);
            var scenes = EditorBuildSettings.scenes.ToList();
            var removed = scenes.RemoveAll(s => PathsEqual(s.path, normalized));
            EditorBuildSettings.scenes = scenes.ToArray();
            cmd.Result.Write(new { path = normalized, removed = removed > 0, count = scenes.Count });
            return ExitCode.Success;
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Refuse a mutating scene op while entering or in play mode, logging why, *before* touching
        /// any state, so the command ends as a clean failure rather than leaving the scene manager
        /// inconsistent. Returns true when the command must stop.
        /// </summary>
        private static bool RefuseInPlayMode(CommandContext cmd, string command)
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                return false;

            cmd.Log.Error(
                $"'{command}' cannot run while the editor is in (or entering) play mode. " +
                $"Exit play mode ({CommandNames.EditorPlaymodeStop}) and retry — no scene state was changed.");
            return true;
        }

        /// <summary>
        /// Map the create_scene <c>template</c> argument to a <see cref="NewSceneSetup"/>. "empty"
        /// (or a null/blank value) keeps the historical blank-scene behavior; "default" seeds Unity's
        /// built-in "3D" template (Main Camera tagged MainCamera + Directional Light). Returns false
        /// for an unknown value.
        /// </summary>
        private static bool TryResolveSceneSetup(string template, out NewSceneSetup setup)
        {
            // Treat a null/blank template as the default ("empty") for parity with an omitted argument.
            var value = string.IsNullOrWhiteSpace(template) ? TemplateEmpty : template.Trim();

            setup = NewSceneSetup.EmptyScene;
            if (string.Equals(value, TemplateEmpty, StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.Equals(value, TemplateDefault, StringComparison.OrdinalIgnoreCase))
            {
                setup = NewSceneSetup.DefaultGameObjects;
                return true;
            }

            return false;
        }

        /// <summary>Resolve an agent path through the sandbox and normalize it to a "*.unity" asset path.</summary>
        private static string ResolveScenePath(string path)
        {
            var normalized = ProjectPaths.Resolve(path, out var error);
            if (normalized == null)
                throw new ArgumentException(error);

            if (!normalized.EndsWith(SceneExtension, StringComparison.OrdinalIgnoreCase))
                normalized += SceneExtension;

            return normalized;
        }

        /// <summary>Create the scene's parent folder chain if missing (mirrors FolderCommands).</summary>
        private static void EnsureParentFolder(string scenePath)
        {
            var parent = Path.GetDirectoryName(scenePath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(parent) || AssetDatabase.IsValidFolder(parent))
                return;

            CreateFolderRecursive(parent);
        }

        private static void CreateFolderRecursive(string assetsPath)
        {
            if (AssetDatabase.IsValidFolder(assetsPath))
                return;

            var parent = Path.GetDirectoryName(assetsPath)?.Replace('\\', '/');
            var name = Path.GetFileName(assetsPath);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
                throw new ArgumentException($"Invalid folder path '{assetsPath}'.");

            if (!AssetDatabase.IsValidFolder(parent))
                CreateFolderRecursive(parent);

            AssetDatabase.CreateFolder(parent, name);
        }

        /// <summary>Build the canonical identity envelope for a scene asset path.</summary>
        private static AuthoringResult DescribeSceneAsset(string scenePath)
        {
            var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
            var result = ObjectResolver.Describe(asset) ?? new AuthoringResult { Type = nameof(SceneAsset) };
            result.AssetPath = scenePath;
            return result;
        }

        /// <summary>Enumerate every open scene (loaded or not) by index.</summary>
        private static IEnumerable<Scene> OpenScenes()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
                yield return SceneManager.GetSceneAt(i);
        }

        /// <summary>Find an open scene by its (normalized) asset path. Returns an invalid Scene when not open.</summary>
        private static Scene FindOpenScene(string normalizedPath)
        {
            foreach (var scene in OpenScenes())
            {
                if (PathsEqual(scene.path, normalizedPath))
                    return scene;
            }

            return default;
        }

        private static bool PathsEqual(string a, string b) =>
            string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

        private static SceneHierarchyNode BuildNode(GameObject go, string hierarchyPath)
        {
            var node = new SceneHierarchyNode
            {
                Name = go.name,
                InstanceId = PipelineUtils.GetObjectId(go),
                HierarchyPath = hierarchyPath,
                ActiveSelf = go.activeSelf,
                Components = go.GetComponents<Component>()
                    // A missing/broken script serializes as a null component; skip it rather than NRE.
                    .Where(c => c != null)
                    .Select(c => c.GetType().Name)
                    .ToList()
            };

            var transform = go.transform;
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i).gameObject;
                node.Children.Add(BuildNode(child, hierarchyPath + "/" + child.name));
            }

            return node;
        }

        #endregion
    }
}
