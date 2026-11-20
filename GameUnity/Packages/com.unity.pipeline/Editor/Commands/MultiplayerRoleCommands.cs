#if UNITY_6000_6_OR_NEWER
using System;
using System.Linq;
using Unity.Multiplayer;
using Unity.Multiplayer.Editor;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor.Authoring;
using Unity.Pipeline.Editor.Commands.Build;
using Unity.Pipeline.Editor.Commands.GameObjects;
using Unity.Pipeline.Models;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.Toolbars;
using UnityEngine;

namespace Unity.Pipeline.Editor.Commands
{
    // Multiplayer Roles commands, on the public Unity.Multiplayer.Editor.EditorMultiplayerRolesManager
    // in UnityEditor.MultiplayerModule. That type is internal to the engine before 6000.6, so the group
    // is compiled out there, the way the Play Mode Scenario commands are before 6000.3. The project-wide
    // settings (enabled, safety checks, server stripping) are in settings.multiplayer_roles.
    static class MultiplayerRoleCommands
    {
        const string Group = "multiplayer.role";

        // Shown in notes: the settings group's set, with the argument that turns roles on.
        const string EnableRolesHint = "settings.multiplayer_roles set settings={\"enabled\":true} confirm=true";
        const string ListComponentTypesCommand = Group + " list_component_types";
        const string SetGameObjectCommand = Group + " set_game_object";
        const string SetComponentCommand = Group + " set_component";

        // The engine's role dropdown in the main toolbar. It refreshes itself only when its own menu
        // changes the role, so a role set from here would leave it showing the old one.
        const string RoleToolbarElementPath = "Multiplayer/Multiplayer Role";

        const string RoleValues = "client | server | client_and_server";

        [CliGroupCommand(Group,
            "Get the multiplayer role the Editor runs as in Play Mode, and whether Multiplayer Roles are enabled for the project.",
            Tags = new[] { "multiplayer/roles" },
            Effect = CommandEffect.ReadOnly)]
        internal static ExitCode GetActive(CommandContext cmd)
        {
            if (!EditorMultiplayerRolesManager.EnableMultiplayerRoles)
                cmd.Log.Info($"Multiplayer Roles are disabled, so the role strips nothing. Enable them with {EnableRolesHint}.");

            cmd.Result.Write(new
            {
                activeRole = EditorMultiplayerRolesManager.ActiveMultiplayerRoleMask.ToString(),
                enabled = EditorMultiplayerRolesManager.EnableMultiplayerRoles
            });
            return ExitCode.Success;
        }

        [CliGroupCommand(Group,
            "Set the multiplayer role the Editor runs as in Play Mode (Edit Mode only). role: " + RoleValues + ".",
            Tags = new[] { "multiplayer/roles" },
            Effect = CommandEffect.Idempotent)]
        internal static ExitCode SetActive(CommandContext cmd,
            [CliArg("role", "Role to run as: " + RoleValues + ".", Required = true)] string role)
        {
            var mask = ParseRole(role);
            if (RefuseWhilePlaying(cmd, Group + " set_active"))
                return ExitCode.Failed;

            var previous = EditorMultiplayerRolesManager.ActiveMultiplayerRoleMask;
            EditorMultiplayerRolesManager.ActiveMultiplayerRoleMask = mask;
            MainToolbar.Refresh(RoleToolbarElementPath);

            if (!EditorMultiplayerRolesManager.EnableMultiplayerRoles)
                cmd.Log.Warning($"Multiplayer Roles are disabled, so the role strips nothing until they are enabled ({EnableRolesHint}).");

            cmd.Result.Write(new
            {
                activeRole = EditorMultiplayerRolesManager.ActiveMultiplayerRoleMask.ToString(),
                previousRole = previous.ToString()
            });
            return ExitCode.Success;
        }

        [CliGroupCommand(Group,
            "Get the roles a GameObject is kept for. For one of its components, use multiplayer.role get_component.",
            Tags = new[] { "multiplayer/roles", "gameobjects" },
            Effect = CommandEffect.ReadOnly)]
        internal static ExitCode GetGameObject(CommandContext cmd,
            [CliArg("target", "Handle of the GameObject.", Required = true)] ObjectRef target)
        {
            var code = ResolveRoleGameObject(cmd, target, out var go);
            if (code != ExitCode.Success)
                return code;

            cmd.Result.Write(DescribeObject(go));
            return ExitCode.Success;
        }

        [CliGroupCommand(Group,
            "Set the roles a GameObject is kept for; it is stripped from the others. Undoable. For one of its components, use multiplayer.role set_component. role: " + RoleValues + ".",
            Tags = new[] { "multiplayer/roles", "gameobjects" },
            Effect = CommandEffect.Idempotent)]
        internal static ExitCode SetGameObject(CommandContext cmd,
            [CliArg("target", "Handle of the GameObject.", Required = true)] ObjectRef target,
            [CliArg("role", "Roles to keep the GameObject for: " + RoleValues + ".", Required = true)] string role)
        {
            var mask = ParseRole(role);
            var code = ResolveRoleGameObject(cmd, target, out var go);
            if (code != ExitCode.Success)
                return code;

            using (new AuthoringUndoScope("Set Multiplayer Role"))
            {
                // The setters register no Undo of their own; record the object the way the Inspector's role field does.
                Undo.RegisterFullObjectHierarchyUndo(go, "Set Multiplayer Role");
                EditorMultiplayerRolesManager.SetMultiplayerRoleMaskForGameObject(go, mask);
                GameObjectCommands.MarkDirty(go);
            }

            cmd.Result.Write(DescribeObject(go));
            return ExitCode.Success;
        }

        [CliGroupCommand(Group,
            "Get the roles a component is kept for. Address it by component handle (e.g. its GlobalObjectId), or by GameObject handle + type.",
            Tags = new[] { "multiplayer/roles", "gameobjects/components" },
            Effect = CommandEffect.ReadOnly)]
        internal static ExitCode GetComponent(CommandContext cmd,
            [CliArg("target", "Handle of the component, OR of its GameObject when 'type' is given.", Required = true)] ObjectRef target,
            [CliArg("type", "Component type name on the target GameObject (omit when 'target' is a component handle).")] string type = null)
        {
            var code = ResolveRoleComponent(cmd, target, type, out var component);
            if (code != ExitCode.Success)
                return code;

            cmd.Result.Write(DescribeObject(component));
            return ExitCode.Success;
        }

        [CliGroupCommand(Group,
            "Set the roles a component is kept for; it is stripped from the others. Address it by component handle (e.g. its GlobalObjectId), or by GameObject handle + type. Undoable. role: " + RoleValues + ".",
            Tags = new[] { "multiplayer/roles", "gameobjects/components" },
            Effect = CommandEffect.Idempotent)]
        internal static ExitCode SetComponent(CommandContext cmd,
            [CliArg("target", "Handle of the component, OR of its GameObject when 'type' is given.", Required = true)] ObjectRef target,
            [CliArg("role", "Roles to keep the component for: " + RoleValues + ".", Required = true)] string role,
            [CliArg("type", "Component type name on the target GameObject (omit when 'target' is a component handle).")] string type = null)
        {
            var mask = ParseRole(role);
            var code = ResolveRoleComponent(cmd, target, type, out var component);
            if (code != ExitCode.Success)
                return code;

            // The engine skips a restricted component with only a console warning; refuse it instead.
            if (Attribute.IsDefined(component.GetType(), typeof(MultiplayerRoleRestrictedAttribute), true))
            {
                cmd.Log.Error($"{component.GetType().Name} is marked [MultiplayerRoleRestricted], so it is never stripped and its role can't be set.");
                return ExitCode.Failed;
            }

            using (new AuthoringUndoScope("Set Multiplayer Role"))
            {
                Undo.RegisterFullObjectHierarchyUndo(component, "Set Multiplayer Role");
                EditorMultiplayerRolesManager.SetMultiplayerRoleMaskForComponent(component, mask);
                GameObjectCommands.MarkDirty(component.gameObject);
            }

            cmd.Result.Write(DescribeObject(component));
            return ExitCode.Success;
        }

        [CliGroupCommand(Group,
            "Get the role a build profile builds for. Defaults to the active build profile, or the active build target when no profile is active.",
            Tags = new[] { "multiplayer/roles", "build" },
            Effect = CommandEffect.ReadOnly)]
        internal static ExitCode GetBuild(CommandContext cmd,
            [CliArg("profile", "Build profile asset path or name (defaults to the active profile or build target).")] string profile = null)
        {
            if (!TryResolveBuildProfile(cmd, profile, out var buildProfile))
                return ExitCode.NotFound;

            cmd.Result.Write(DescribeBuildRole(buildProfile));
            return ExitCode.Success;
        }

        [CliGroupCommand(Group,
            "Set the role a build profile builds for. Defaults to the active build profile, or the active build target when no profile is active. role: " + RoleValues + ".",
            Tags = new[] { "multiplayer/roles", "build" },
            Effect = CommandEffect.Idempotent)]
        internal static ExitCode SetBuild(CommandContext cmd,
            [CliArg("role", "Role to build for: " + RoleValues + ".", Required = true)] string role,
            [CliArg("profile", "Build profile asset path or name (defaults to the active profile or build target).")] string profile = null)
        {
            var mask = ParseRole(role);

            if (!TryResolveBuildProfile(cmd, profile, out var buildProfile))
                return ExitCode.NotFound;

            if (buildProfile != null)
            {
                EditorMultiplayerRolesManager.SetMultiplayerRoleForBuildProfile(buildProfile, mask);
                EditorUtility.SetDirty(buildProfile);
                AssetDatabase.SaveAssetIfDirty(buildProfile);
            }
            else
            {
                var target = EditorUserBuildSettings.activeBuildTarget;
                if (IsStandalone(target))
                    EditorMultiplayerRolesManager.SetMultiplayerRoleForClassicTarget(target, EditorUserBuildSettings.standaloneBuildSubtarget, mask);
                else
                    EditorMultiplayerRolesManager.SetMultiplayerRoleForClassicTarget(target, mask);
            }

            cmd.Result.Write(DescribeBuildRole(buildProfile));
            return ExitCode.Success;
        }

        [CliGroupCommand(Group,
            "List the component types the project automatically keeps for specific roles, beyond the server's audio, rendering and UI stripping.",
            Tags = new[] { "multiplayer/roles" },
            Effect = CommandEffect.ReadOnly)]
        internal static ExitCode ListComponentTypes(CommandContext cmd)
        {
            var types = EditorMultiplayerRolesManager.AutomaticSelection.GetCustomComponents()
                .Select(entry => new { type = entry.Key.FullName, role = entry.Value.ToString() })
                .OrderBy(entry => entry.type, StringComparer.Ordinal);

            foreach (var type in types)
                cmd.Result.WriteRecord(type);

            return ExitCode.Success;
        }

        [CliGroupCommand(Group,
            "Keep every component of a type for the given roles only, project-wide. client_and_server keeps it everywhere. role: " + RoleValues + ".",
            Tags = new[] { "multiplayer/roles" },
            Effect = CommandEffect.Idempotent)]
        internal static ExitCode SetComponentType(CommandContext cmd,
            [CliArg("type", "Component type name (e.g. 'AudioSource' or 'UnityEngine.Camera').", Required = true)] string type,
            [CliArg("role", "Roles to keep the component type for: " + RoleValues + ".", Required = true)] string role)
        {
            var mask = ParseRole(role);
            var componentType = TypeResolver.ResolveComponentType(type);
            if (componentType == null)
            {
                cmd.Log.Error($"Could not resolve component type '{type}'.");
                return ExitCode.NotFound;
            }

            if (RefuseWhilePlaying(cmd, Group + " set_component_type"))
                return ExitCode.Failed;

            EditorMultiplayerRolesManager.AutomaticSelection.SetMultiplayerRoleMaskForComponentType(componentType, mask);

            cmd.Log.Info($"List every type with an automatic role with {ListComponentTypesCommand}.");
            cmd.Result.Write(new
            {
                type = componentType.FullName,
                role = EditorMultiplayerRolesManager.AutomaticSelection.GetMultiplayerRoleMaskForComponentType(componentType).ToString()
            });
            return ExitCode.Success;
        }

        // Accepts the MultiplayerRoleFlags names in any case, ignoring whitespace, '_' and '-', so
        // "client_and_server", "ClientAndServer" and "client-and-server" all parse. Anything else,
        // including the flags' numeric values, is rejected.
        internal static MultiplayerRoleFlags ParseRole(string role)
        {
            var key = new string((role ?? string.Empty)
                .Where(c => !char.IsWhiteSpace(c) && c != '_' && c != '-')
                .ToArray());

            foreach (MultiplayerRoleFlags candidate in Enum.GetValues(typeof(MultiplayerRoleFlags)))
            {
                if (string.Equals(candidate.ToString(), key, StringComparison.OrdinalIgnoreCase))
                    return candidate;
            }

            throw new ArgumentException($"Unknown multiplayer role '{role}'. Use {RoleValues}.");
        }

        static ExitCode ResolveRoleGameObject(CommandContext cmd, ObjectRef target, out GameObject go)
        {
            go = null;
            if (!ObjectResolver.TryResolve(target, out var obj, out var error))
            {
                cmd.Log.Error($"Could not resolve 'target': {error}");
                return ExitCode.NotFound;
            }

            if (obj is Component component)
            {
                cmd.Log.Error($"'target' is a {component.GetType().Name} component, not a GameObject. Use {SetComponentCommand} for a component.");
                return ExitCode.InvalidUsage;
            }

            go = obj as GameObject;
            if (go == null)
            {
                cmd.Log.Error($"'target' did not resolve to a GameObject (got {obj.GetType().Name}).");
                return ExitCode.InvalidUsage;
            }

            return ExitCode.Success;
        }

        static ExitCode ResolveRoleComponent(CommandContext cmd, ObjectRef target, string type, out Component component)
        {
            component = null;

            // Name the GameObject command too, since a GameObject handle without a type is most
            // likely meant for the GameObject itself.
            if (string.IsNullOrEmpty(type)
                && ObjectResolver.TryResolve(target, out var obj, out _)
                && obj is GameObject)
            {
                cmd.Log.Error($"'target' is a GameObject. Pass 'type' to pick one of its components, or use {SetGameObjectCommand} for the GameObject itself.");
                return ExitCode.InvalidUsage;
            }

            var code = ComponentCommands.TryResolveComponent(target, type, out component, out var error);
            if (code != ExitCode.Success)
                cmd.Log.Error(error);
            return code;
        }

        static object DescribeObject(UnityEngine.Object obj)
        {
            var role = obj is GameObject go
                ? EditorMultiplayerRolesManager.GetMultiplayerRoleMaskForGameObject(go)
                : EditorMultiplayerRolesManager.GetMultiplayerRoleMaskForComponent((Component)obj);

            return new { target = ObjectResolver.Describe(obj), role = role.ToString() };
        }

        // A null buildProfile means the active build target.
        static object DescribeBuildRole(BuildProfile buildProfile)
        {
            if (buildProfile != null)
            {
                return new
                {
                    profile = AssetDatabase.GetAssetPath(buildProfile),
                    target = (string)null,
                    role = EditorMultiplayerRolesManager.GetMultiplayerRoleForBuildProfile(buildProfile).ToString()
                };
            }

            var target = EditorUserBuildSettings.activeBuildTarget;
            var role = IsStandalone(target)
                ? EditorMultiplayerRolesManager.GetMultiplayerRoleForClassicTarget(target, EditorUserBuildSettings.standaloneBuildSubtarget)
                : EditorMultiplayerRolesManager.GetMultiplayerRoleForClassicTarget(target);

            return new
            {
                profile = (string)null,
                target = IsStandalone(target) ? $"{target} ({EditorUserBuildSettings.standaloneBuildSubtarget})" : target.ToString(),
                role = role.ToString()
            };
        }

        // A null buildProfile means "use the active build target": no profile was named and none is
        // active. False when a named profile doesn't exist.
        static bool TryResolveBuildProfile(CommandContext cmd, string profile, out BuildProfile buildProfile)
        {
            if (string.IsNullOrEmpty(profile))
            {
                buildProfile = BuildProfile.GetActiveBuildProfile();
                return true;
            }

            var info = BuildProfiles.FindByPath(profile) ?? BuildProfiles.FindByName(profile);
            buildProfile = info == null ? null : AssetDatabase.LoadAssetAtPath<BuildProfile>(AssetDatabase.GUIDToAssetPath(info.Guid));
            if (buildProfile != null)
                return true;

            cmd.Log.Error($"No build profile '{profile}'. Pass an asset path or a name from {CommandNames.BuildListProfiles}.");
            return false;
        }

        static bool IsStandalone(BuildTarget target) =>
            BuildPipeline.GetBuildTargetGroup(target) == BuildTargetGroup.Standalone;

        // Roles decide what is stripped when the Editor enters Play Mode, so a change made in Play
        // Mode would not apply until the next entry; refuse it rather than appear to succeed.
        static bool RefuseWhilePlaying(CommandContext cmd, string command)
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                return false;

            cmd.Log.Error(
                $"'{command}' cannot run while the Editor is in (or entering) Play Mode, where roles have already been applied. " +
                $"Exit Play Mode ({CommandNames.EditorPlaymodeStop}) and retry. Nothing was changed.");
            return true;
        }
    }
}
#endif
