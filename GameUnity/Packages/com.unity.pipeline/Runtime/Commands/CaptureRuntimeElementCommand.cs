#if UNITY_6000_7_OR_NEWER
using System.Collections.Generic;
using System.Linq;
using Unity.Pipeline.Commands;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.Pipeline.Runtime.Commands
{
    /// <summary>
    /// Captures a UI Toolkit <see cref="VisualElement"/> from a live runtime panel to a PNG and
    /// returns it base64-encoded (and writes it to disk). Runtime-only so it is available in a
    /// development Player as well as the editor's Play mode.
    ///
    /// A runtime panel is hosted by either a <see cref="UIDocument"/> or a <see cref="PanelRenderer"/>.
    /// The panel is identified by name (the <see cref="PanelSettings"/> asset name or the host
    /// GameObject name) rather than an asset path, because a Player has no AssetDatabase.
    /// </summary>
    static class CaptureRuntimeElementCommand
    {
        const string Group = "capture";

        [CliGroupCommand(Group,
            "Capture a UI Toolkit VisualElement (by selector) from a live runtime panel (UIDocument or PanelRenderer) to a PNG; returns path + base64.",
            MainThreadRequired = true,
            Tags = new[] { "capture", "ui", "runtime" },
            Effect = CommandEffect.Destructive)]
        internal static ExitCode RuntimeElement(CommandContext cmd,
            [CliArg("panel", "Name of the target panel: matches the PanelSettings asset name or the host GameObject name (UIDocument or PanelRenderer). Optional when exactly one panel exists.")] string panel = "",
            [CliArg("selector", "Element selector: '#name', '.class', a type name (e.g. Button), descendant (space) / child ('>') chains, optional pseudo-states (:checked,:hover,:focus,:active,:enabled,:disabled,:not(...)).", Required = true)] string selector = "",
            [CliArg("output-path", "Output PNG path (absolute, or relative to Application.persistentDataPath). Defaults to a timestamped file under Application.persistentDataPath.")] string output = "")
        {
            if (string.IsNullOrWhiteSpace(selector))
            {
                cmd.Log.Error("A 'selector' is required.");
                return ExitCode.InvalidUsage;
            }

            var hosts = GatherHosts();
            if (hosts.Count == 0)
            {
                cmd.Log.Error("No live runtime UI panels found (UIDocument or PanelRenderer). Show the UI first.");
                return ExitCode.NotFound;
            }

            List<Host> matched;
            string resolvedName;
            if (string.IsNullOrWhiteSpace(panel))
            {
                if (hosts.Count > 1)
                {
                    var names = string.Join(", ", hosts.Select(h => h.DisplayName).Distinct());
                    cmd.Log.Error($"Multiple runtime panels exist; specify --panel as one of: {names}.");
                    return ExitCode.InvalidUsage;
                }

                matched = hosts;
                resolvedName = hosts[0].DisplayName;
            }
            else
            {
                matched = hosts.Where(h => h.PanelSettingsName == panel || h.GameObjectName == panel).ToList();
                if (matched.Count == 0)
                {
                    var names = string.Join(", ", hosts.Select(h => h.DisplayName).Distinct());
                    cmd.Log.Error($"No live runtime panel named '{panel}'. Available: {names}.");
                    return ExitCode.NotFound;
                }

                resolvedName = panel;
            }

            // Lower sorting order is drawn first (further back); query in that order so the topmost
            // match is found last only if earlier panels miss. Roots that can't be resolved are skipped.
            var roots = matched
                .OrderBy(h => h.SortingOrder)
                .Select(h => h.GetRoot())
                .Where(r => r != null)
                .ToList();

            if (roots.Count == 0)
            {
                cmd.Log.Error($"Panel '{resolvedName}' has no initialized root VisualElement yet. Show the UI first.");
                return ExitCode.Failed;
            }

            var element = VisualElementCaptureSupport.ResolveElement(roots, selector, out _);
            if (element == null)
            {
                cmd.Log.Error($"No element matched selector '{selector}' in panel '{resolvedName}'.");
                return ExitCode.NotFound;
            }

            var dir = Application.persistentDataPath;
            var captured = VisualElementCaptureSupport.CaptureAndRespond(
                element, selector, $"panel:{resolvedName}", output,
                relativeBaseDir: dir, defaultDir: dir, prefix: "element");
            if (!captured.Success)
            {
                cmd.Log.Error(captured.Message);
                return ExitCode.Failed;
            }

            cmd.Log.Info(captured.Message);
            cmd.Result.Write(new
            {
                selector = captured.Selector,
                source = captured.Source,
                path = captured.Path,
                width = captured.Width,
                height = captured.Height,
                encoding = captured.Encoding,
                base64 = captured.Base64,
                bytes = captured.Bytes
            });
            return ExitCode.Success;
        }

        /// <summary>A live runtime UI host (UIDocument or PanelRenderer) and how to reach its root.</summary>
        class Host
        {
            public string PanelSettingsName;
            public string GameObjectName;
            public float SortingOrder;
            public System.Func<VisualElement> GetRoot;

            public string DisplayName => PanelSettingsName ?? GameObjectName;
        }

        static List<Host> GatherHosts()
        {
            var hosts = new List<Host>();

            foreach (var doc in PipelineUtils.FindObjectsByType<UIDocument>())
            {
                var d = doc;
                hosts.Add(new Host
                {
                    PanelSettingsName = d.panelSettings != null ? d.panelSettings.name : null,
                    GameObjectName = d.gameObject.name,
                    SortingOrder = d.sortingOrder,
                    GetRoot = () => d.rootVisualElement
                });
            }

            // PanelRenderer is a 6000.5+ UI Toolkit API; on older editors only UIDocument panels exist.
            foreach (var renderer in PipelineUtils.FindObjectsByType<PanelRenderer>())
            {
                var r = renderer;
                hosts.Add(new Host
                {
                    PanelSettingsName = r.panelSettings != null ? r.panelSettings.name : null,
                    GameObjectName = r.gameObject.name,
                    SortingOrder = r.sortingOrder,
                    GetRoot = () => GetPanelRendererRoot(r)
                });
            }

            return hosts;
        }

        // PanelRenderer.rootVisualElement is internal; the public route to its root is a UI-reload
        // callback, which fires synchronously when the panel is already initialized. Register, capture
        // the root, and immediately unregister. PanelRenderer is a 6000.5+ API.
        static VisualElement GetPanelRendererRoot(PanelRenderer renderer)
        {
            VisualElement captured = null;
            PanelRenderer.VersionedUIReloadCallback callback = (pr, root, version) => captured = root;
            renderer.RegisterUIReloadCallback(callback);
            renderer.UnregisterUIReloadCallback(callback);
            return captured;
        }
    }
}
#endif
