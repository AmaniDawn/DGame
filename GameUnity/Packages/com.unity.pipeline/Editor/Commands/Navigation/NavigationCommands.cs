using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor.Authoring;
using Unity.Pipeline.Models;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.Pipeline.Editor.Commands.Navigation
{
    /// <summary>
    /// Navigation and targeting commands (CLI-200): read and drive the Editor selection, and run
    /// Unity Search queries. The selection commands write one record per selected object, each the
    /// canonical <see cref="AuthoringResult"/> identity (via <see cref="ObjectResolver.Describe"/>) so
    /// an agent can reference it in a follow-up call; search writes one record per hit. All commands
    /// are read-only or non-destructive (set_selection only changes the Editor selection, which carries
    /// no undo/safety policy), and run on the main thread.
    /// </summary>
    static class NavigationCommands
    {
        const string Group = "gameobject";
        const string EditorGroup = "editor";

        private const int MaxSearchResults = 200;

        [CliGroupCommand(Group, "Read the current Editor selection as structured object identities.", Tags = new[] { "navigation" }, Effect = CommandEffect.ReadOnly)]
        internal static ExitCode GetSelection(CommandContext cmd)
        {
            WriteSelection(cmd);
            return ExitCode.Success;
        }

        /// <summary>
        /// <para>Set the Editor selection from instance ids and/or asset paths.</para>
        /// <para>
        /// We accept simple <paramref name="instanceIds"/> / <paramref name="paths"/> rather than full
        /// <see cref="ObjectRef"/>[] handles because nested-object parameter schemas (arrays of objects)
        /// are pending CAT-2508. Once that lands this can take an <see cref="ObjectRef"/>[] and route
        /// each through <see cref="ObjectResolver.TryResolve"/> for the full handle surface.
        /// </para>
        /// </summary>
        [CliGroupCommand(Group, "Set the Editor selection to the given assets/scene objects.", Tags = new[] { "navigation" }, Effect = CommandEffect.Idempotent)]
        internal static ExitCode SetSelection(CommandContext cmd,
            [CliArg("instance-ids", "Scene/loaded object instance IDs to select.")] ObjectId[] instanceIds = null,
            [CliArg("paths", "Asset paths to select (e.g. Assets/Foo.prefab).")] string[] paths = null)
        {
            var resolved = new List<Object>();
            var unresolved = new List<string>();

            if (instanceIds != null)
            {
                foreach (var id in instanceIds)
                {
                    var obj = PipelineUtils.IdToObject(id);
                    if (obj != null)
                        resolved.Add(obj);
                    else
                        unresolved.Add($"instanceId:{id}");
                }
            }

            if (paths != null)
            {
                foreach (var path in paths)
                {
                    var obj = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadMainAssetAtPath(path);
                    if (obj != null)
                        resolved.Add(obj);
                    else if (path == null)
                        unresolved.Add("<null>");
                    else if (path.Length == 0)
                        unresolved.Add("<empty>");
                    else
                        unresolved.Add(path);
                }
            }

            var hadInputs = (instanceIds != null && instanceIds.Length > 0) ||
                            (paths != null && paths.Length > 0);

            // Inputs were given but none resolved: that's a hard error, not a silent clear.
            if (resolved.Count == 0 && hadInputs)
            {
                cmd.Log.Error($"No objects resolved from the given inputs; the selection is unchanged. Unresolved: {string.Join(", ", unresolved)}");
                return ExitCode.NotFound;
            }

            if (unresolved.Count > 0)
                cmd.Log.Warning($"Skipped {unresolved.Count} input(s) that did not resolve: {string.Join(", ", unresolved)}");

            // No inputs at all is a valid request to clear the selection.
            Selection.objects = resolved.ToArray();
            Selection.activeObject = resolved.FirstOrDefault();

            WriteSelection(cmd);
            return ExitCode.Success;
        }

        [CliGroupCommand(EditorGroup, "Run a Unity Search query and return structured results.", Tags = new[] { "navigation" }, Effect = CommandEffect.ReadOnly)]
        internal static ExitCode Search(CommandContext cmd,
            [CliArg("query", "Unity Search query string, e.g. 't:Material', 'p: my asset', 'h: Main Camera'.", Required = true)] string query,
            [CliArg("limit", "Max results to return (capped 200).")] int limit = 50)
        {
            if (string.IsNullOrEmpty(query))
            {
                cmd.Log.Error("query must be a non-empty Unity Search query string.");
                return ExitCode.InvalidUsage;
            }

            var clamped = Mathf.Clamp(limit, 0, MaxSearchResults);

            // A non-positive limit can't return any rows; skip the potentially expensive request.
            if (clamped <= 0)
                return ExitCode.Success;

            var written = 0;
            var skipped = 0;
            ISearchList list = null;
            try
            {
                // Synchronous request returns the items already resolved on the main thread.
                list = SearchService.Request(query, SearchFlags.Synchronous);

                foreach (var item in list)
                {
                    if (written >= clamped)
                        break;
                    if (item == null)
                        continue;

                    // One malformed item must not fail the whole call.
                    SearchResultItem mapped;
                    try
                    {
                        mapped = MapItem(item);
                    }
                    catch
                    {
                        skipped++;
                        continue;
                    }

                    cmd.Result.WriteRecord(mapped);
                    written++;
                }
            }
            catch (Exception ex)
            {
                cmd.Log.Error($"Unity Search query '{query}' failed: {ex.Message}");
                return ExitCode.Failed;
            }
            finally
            {
                // The Synchronous Request overload returns an ISearchList that owns a SearchContext;
                // dispose it when present so we don't leak the context.
                (list as IDisposable)?.Dispose();
            }

            if (skipped > 0)
                cmd.Log.Warning($"Skipped {skipped} search result(s) that could not be read.");
            return ExitCode.Success;
        }

        private static SearchResultItem MapItem(SearchItem item)
        {
            string label;
            try
            {
                label = item.GetLabel(item.context, true);
            }
            catch
            {
                label = null;
            }

            if (string.IsNullOrEmpty(label))
                label = item.label ?? item.id;

            string description = null;
            try
            {
                description = item.GetDescription(item.context, true);
            }
            catch
            {
                // leave description null
            }

            string path = null;
            try
            {
                var obj = item.ToObject();
                if (obj != null)
                {
                    var assetPath = AssetDatabase.GetAssetPath(obj);
                    if (!string.IsNullOrEmpty(assetPath))
                        path = assetPath;
                }
            }
            catch
            {
                // leave path null
            }

            return new SearchResultItem
            {
                Id = item.id,
                Label = label,
                Description = description,
                Provider = item.provider?.id,
                Path = path
            };
        }

        /// <summary>
        /// Writes the current <see cref="Selection"/> as one record per selected object, marking the
        /// active one. An active object outside <see cref="Selection.objects"/> is written last.
        /// </summary>
        private static void WriteSelection(CommandContext cmd)
        {
            var active = Selection.activeObject;
            var wroteActive = false;
            foreach (var obj in Selection.objects ?? Array.Empty<Object>())
            {
                var described = ObjectResolver.Describe(obj);
                if (described == null)
                    continue;
                var isActive = active != null && obj == active;
                wroteActive |= isActive;
                cmd.Result.WriteRecord(SelectedObject.From(described, isActive));
            }

            if (!wroteActive && active != null)
            {
                var described = ObjectResolver.Describe(active);
                if (described != null)
                    cmd.Result.WriteRecord(SelectedObject.From(described, true));
            }
        }
    }

    /// <summary>
    /// One selected object: its canonical <see cref="AuthoringResult"/> identity, and whether it is
    /// the active object of the selection.
    /// </summary>
    [Serializable]
    class SelectedObject : AuthoringResult
    {
        /// <summary>True for the selection's active object.</summary>
        [JsonProperty("active")]
        public bool Active { get; set; }

        public static SelectedObject From(AuthoringResult described, bool active) => new SelectedObject
        {
            GlobalId = described.GlobalId,
            AssetPath = described.AssetPath,
            Guid = described.Guid,
            FileId = described.FileId,
            InstanceId = described.InstanceId,
            HierarchyPath = described.HierarchyPath,
            Type = described.Type,
            Active = active
        };
    }

    /// <summary>
    /// One Unity Search result, flattened to portable fields. <see cref="Path"/> is populated only
    /// when the item resolves to an asset object.
    /// </summary>
    [Serializable]
    class SearchResultItem
    {
        /// <summary>Provider-specific item id (e.g. an asset path or scene object id).</summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>Display label for the item.</summary>
        [JsonProperty("label")]
        public string Label { get; set; }

        /// <summary>Longer description, when the provider supplies one (may be null).</summary>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>Id of the search provider that produced this item (e.g. "asset", "scene").</summary>
        [JsonProperty("provider")]
        public string Provider { get; set; }

        /// <summary>Project-relative asset path, when the item resolves to an asset (else null).</summary>
        [JsonProperty("path")]
        public string Path { get; set; }
    }
}
