using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor.Authoring;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Unity.Pipeline.Editor.Commands.Capture
{
    /// <summary>
    /// Visual-feedback commands (CLI-199): render a camera or the Scene View to a PNG and return it
    /// base64-encoded, so an agent can "see" the editor without a display. Optionally writes the PNG
    /// under the project (sandboxed via <see cref="ProjectPaths"/>) and refreshes the AssetDatabase.
    ///
    /// Captures require a GPU; in batchmode/headless the device type is
    /// <see cref="GraphicsDeviceType.Null"/> and the commands throw so callers get a clear message
    /// instead of an empty image.
    /// </summary>
    public static class CaptureCommands
    {
        private const int MaxDimension = 4096;

        [CliCommand("capture_game_view", "Render a camera to a PNG and return it base64-encoded.")]
        public static CaptureResult CaptureGameView(
            [CliArg("width", "Output width in px (default 1280; capped 4096).")] int width = 1280,
            [CliArg("height", "Output height in px (default 720; capped 4096).")] int height = 720,
            [CliArg("camera", "Optional camera name; defaults to Camera.main, else the first enabled camera.")] string camera = null,
            [CliArg("save_path", "Optional project-relative path to also write the PNG (e.g. Screenshots/foo.png).")] string savePath = null)
        {
            GuardHasGpu();

            var cam = ResolveCamera(camera);
            if (cam == null)
                throw new ArgumentException("No camera found to capture.");

            var w = Mathf.Clamp(width, 1, MaxDimension);
            var h = Mathf.Clamp(height, 1, MaxDimension);

            var png = EncodeCameraToPng(cam, w, h);
            var savedPath = WriteIfRequested(png, savePath);

            return new CaptureResult
            {
                Width = w,
                Height = h,
                Encoding = "png",
                Base64 = Convert.ToBase64String(png),
                Bytes = png.Length,
                Source = $"camera:{cam.name}",
                SavedPath = savedPath
            };
        }

        [CliCommand("capture_scene_view", "Render the active Scene View to a PNG (base64).")]
        public static CaptureResult CaptureSceneView(
            [CliArg("width", "Output width in px (default 1280; capped 4096).")] int width = 1280,
            [CliArg("height", "Output height in px (default 720; capped 4096).")] int height = 720,
            [CliArg("save_path", "Optional project-relative path to also write the PNG (e.g. Screenshots/foo.png).")] string savePath = null)
        {
            GuardHasGpu();

            var sv = SceneView.lastActiveSceneView;
            if (sv == null || sv.camera == null)
                throw new ArgumentException("No active Scene View to capture.");

            var w = Mathf.Clamp(width, 1, MaxDimension);
            var h = Mathf.Clamp(height, 1, MaxDimension);

            var png = EncodeCameraToPng(sv.camera, w, h);
            var savedPath = WriteIfRequested(png, savePath);

            return new CaptureResult
            {
                Width = w,
                Height = h,
                Encoding = "png",
                Base64 = Convert.ToBase64String(png),
                Bytes = png.Length,
                Source = "sceneView",
                SavedPath = savedPath
            };
        }

        /// <summary>Throw when no GPU is available (batchmode/headless), where a render would be blank.</summary>
        private static void GuardHasGpu()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                throw new InvalidOperationException("No GPU available (batchmode/headless); cannot capture.");
        }

        /// <summary>
        /// Resolve the camera to capture: by name (if provided), else <see cref="Camera.main"/>,
        /// else the first enabled camera. Returns null when nothing matches.
        /// </summary>
        private static Camera ResolveCamera(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                // Camera.allCameras only includes enabled cameras; also scan all loaded cameras so a
                // disabled-but-named camera can still be targeted explicitly.
                var byName = Camera.allCameras.FirstOrDefault(c => c.name == name)
                    ?? PipelineUtils.FindObjectsByType<Camera>().FirstOrDefault(c => c.name == name);
                return byName;
            }

            if (Camera.main != null)
                return Camera.main;

            return Camera.allCameras.FirstOrDefault();
        }

        /// <summary>
        /// Render <paramref name="cam"/> off-screen into a temporary RenderTexture and encode the
        /// result to PNG bytes. The camera's target texture and the active RenderTexture are restored
        /// in a finally block so capturing never leaves global render state dirty.
        /// </summary>
        private static byte[] EncodeCameraToPng(Camera cam, int w, int h)
        {
            var rt = RenderTexture.GetTemporary(w, h, 24);
            var prevTarget = cam.targetTexture;
            var prevActive = RenderTexture.active;
            try
            {
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;

                var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                try
                {
                    tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                    tex.Apply();
                    return ImageConversion.EncodeToPNG(tex);
                }
                finally
                {
                    Object.DestroyImmediate(tex);
                }
            }
            finally
            {
                cam.targetTexture = prevTarget;
                RenderTexture.active = prevActive;
                RenderTexture.ReleaseTemporary(rt);
            }
        }

        /// <summary>
        /// When <paramref name="savePath"/> is set, validate it through the project-path sandbox,
        /// write the PNG to its absolute filesystem location, refresh the AssetDatabase, and return
        /// the resolved project-relative asset path. Returns null when no path was requested.
        /// </summary>
        private static string WriteIfRequested(byte[] png, string savePath)
        {
            if (string.IsNullOrEmpty(savePath))
                return null;

            var resolved = ProjectPaths.Resolve(savePath, out var err);
            if (resolved == null)
                throw new ArgumentException(err);

            // ProjectPaths.Resolve returns a project-relative path; combine it with the project root
            // (the folder that contains Assets/) to get the absolute file to write.
            var absolute = Path.Combine(ProjectPaths.ProjectRoot, resolved);
            var directory = Path.GetDirectoryName(absolute);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllBytes(absolute, png);
            AssetDatabase.Refresh();
            return resolved;
        }
    }

    /// <summary>
    /// Result of a capture command: the PNG payload (base64), its dimensions, the source it was
    /// rendered from, and the project-relative path it was written to (null when not saved).
    /// </summary>
    [Serializable]
    public class CaptureResult
    {
        /// <summary>Rendered width in pixels (after clamping).</summary>
        [JsonProperty("width")]
        public int Width { get; set; }

        /// <summary>Rendered height in pixels (after clamping).</summary>
        [JsonProperty("height")]
        public int Height { get; set; }

        /// <summary>Image encoding; always "png".</summary>
        [JsonProperty("encoding")]
        public string Encoding { get; set; }

        /// <summary>Base64-encoded PNG bytes.</summary>
        [JsonProperty("base64")]
        public string Base64 { get; set; }

        /// <summary>Length of the raw PNG byte array.</summary>
        [JsonProperty("bytes")]
        public int Bytes { get; set; }

        /// <summary>What was captured, e.g. "camera:Main Camera" or "sceneView".</summary>
        [JsonProperty("source")]
        public string Source { get; set; }

        /// <summary>Project-relative path the PNG was also written to, or null.</summary>
        [JsonProperty("savedPath")]
        public string SavedPath { get; set; }
    }
}
