#if UNITY_6000_7_OR_NEWER
using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor;
using Unity.Pipeline.Editor.Commands;
using Unity.Pipeline.Runtime.Commands;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace Unity.Pipeline.Tests.Editor
{
    /// <summary>
    /// Tests for the visual-element capture commands. The selector resolver
    /// (<see cref="VisualElementCaptureSupport.ResolveElement(VisualElement,string)"/>) is the
    /// riskiest new logic and is GPU-independent, so it gets the most coverage; the
    /// <c>capture_editor_element</c> command is covered for its validation/miss branches plus a
    /// GPU-gated happy path.
    /// </summary>
    public class CaptureVisualElementCommandsTests
    {
        // -- Selector resolver -------------------------------------------------------------------

        static VisualElement BuildTree()
        {
            // root
            //   #toolbar .toolbar
            //     #search Button .primary    (direct child of toolbar)
            //   #content
            //     #title Label .big          (disabled)
            var root = new VisualElement { name = "root" };

            var toolbar = new VisualElement { name = "toolbar" };
            toolbar.AddToClassList("toolbar");
            var search = new Button { name = "search" };
            search.AddToClassList("primary");
            toolbar.Add(search);

            var content = new VisualElement { name = "content" };
            var title = new Label { name = "title" };
            title.AddToClassList("big");
            title.SetEnabled(false); // sets the :disabled pseudo-state
            content.Add(title);

            root.Add(toolbar);
            root.Add(content);
            return root;
        }

        [Test]
        public void Resolve_ById()
        {
            var root = BuildTree();
            var e = VisualElementCaptureSupport.ResolveElement(root, "#search");
            Assert.IsNotNull(e);
            Assert.AreEqual("search", e.name);
        }

        [Test]
        public void Resolve_ByClass()
        {
            var root = BuildTree();
            var e = VisualElementCaptureSupport.ResolveElement(root, ".toolbar");
            Assert.IsNotNull(e);
            Assert.AreEqual("toolbar", e.name);
        }

        [Test]
        public void Resolve_ByType()
        {
            var root = BuildTree();
            var e = VisualElementCaptureSupport.ResolveElement(root, "Label");
            Assert.IsNotNull(e);
            Assert.AreEqual("title", e.name);
            Assert.IsInstanceOf<Label>(e);
        }

        [Test]
        public void Resolve_TypeWithNameAndClass()
        {
            var root = BuildTree();
            var e = VisualElementCaptureSupport.ResolveElement(root, "Button#search.primary");
            Assert.IsNotNull(e);
            Assert.AreEqual("search", e.name);
            Assert.IsInstanceOf<Button>(e);
        }

        [Test]
        public void Resolve_DescendantChain()
        {
            var root = BuildTree();
            var e = VisualElementCaptureSupport.ResolveElement(root, ".toolbar #search");
            Assert.IsNotNull(e);
            Assert.AreEqual("search", e.name);
        }

        [Test]
        public void Resolve_ChildCombinator()
        {
            var root = BuildTree();
            // search IS a direct child of toolbar.
            Assert.IsNotNull(VisualElementCaptureSupport.ResolveElement(root, "#toolbar > #search"));
            // title is NOT a direct child of toolbar, so the child combinator must miss.
            Assert.IsNull(VisualElementCaptureSupport.ResolveElement(root, "#toolbar > #title"));
        }

        [Test]
        public void Resolve_PseudoDisabledAndEnabled()
        {
            var root = BuildTree();
            Assert.IsNotNull(VisualElementCaptureSupport.ResolveElement(root, "#title:disabled"),
                "title is disabled, so :disabled should match");
            Assert.IsNull(VisualElementCaptureSupport.ResolveElement(root, "#title:enabled"),
                "title is disabled, so :enabled should not match");
            Assert.IsNull(VisualElementCaptureSupport.ResolveElement(root, "#title:not(disabled)"),
                ":not(disabled) should not match a disabled element");
        }

        [Test]
        public void Resolve_Miss_ReturnsNull()
        {
            var root = BuildTree();
            Assert.IsNull(VisualElementCaptureSupport.ResolveElement(root, "#does-not-exist"));
        }

        [Test]
        public void Resolve_InvalidSelector_ReturnsNull()
        {
            var root = BuildTree();
            Assert.IsNull(VisualElementCaptureSupport.ResolveElement(root, "#"), "dangling '#' is invalid");
            Assert.IsNull(VisualElementCaptureSupport.ResolveElement(root, ""), "empty selector");
        }

        // -- capture_editor_element command ------------------------------------------------------

        [Test]
        public void EditorCommand_IsDiscovered()
        {
            CommandRegistry.SetDiscovery(new TypeCacheCommandDiscovery());
            var cmd = CommandRegistry.DiscoverCommands().FirstOrDefault(c => c.Name == "capture_editor_element");
            Assert.IsNotNull(cmd, "capture_editor_element should be discovered");
            Assert.IsTrue(cmd.MainThreadRequired);
        }

        [Test]
        public void RuntimeCommand_IsDiscovered_AndRuntimeOnly()
        {
            CommandRegistry.SetDiscovery(new TypeCacheCommandDiscovery());
            var cmd = CommandRegistry.DiscoverCommands().FirstOrDefault(c => c.Name == "capture_runtime_element");
            Assert.IsNotNull(cmd, "capture_runtime_element should be discovered");
            Assert.IsTrue(cmd.RuntimeOnly, "runtime capture command should be RuntimeOnly");
        }

        [Test]
        public void EditorCommand_MissingWindow_Fails()
        {
            var result = CaptureEditorElementCommand.CaptureEditorElement(window: "", selector: "#x");
            Assert.IsFalse(result.Success);
            StringAssert.Contains("window", result.Message);
        }

        [Test]
        public void EditorCommand_UnknownWindow_Fails()
        {
            var result = CaptureEditorElementCommand.CaptureEditorElement(
                window: "ThisWindowTypeDoesNotExist12345", selector: "#x");
            Assert.IsFalse(result.Success);
            StringAssert.Contains("No open EditorWindow", result.Message);
        }

        [Test]
        public void EditorCommand_SelectorMiss_Fails()
        {
            var win = ScriptableObject.CreateInstance<TestCaptureWindow>();
            try
            {
                win.Show();
                var result = CaptureEditorElementCommand.CaptureEditorElement(
                    window: nameof(TestCaptureWindow), selector: "#no-such-element");
                Assert.IsFalse(result.Success);
                StringAssert.Contains("No element matched", result.Message);
            }
            finally
            {
                win.Close();
            }
        }

        [Test]
        public void EditorCommand_HappyPath_WritesPngAndBase64()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                Assert.Ignore("No GPU available; capture readback is not meaningful in this environment.");

            var win = ScriptableObject.CreateInstance<TestCaptureWindow>();
            var output = Path.Combine(Path.GetTempPath(), $"pipeline_ve_{System.Guid.NewGuid():N}.png");
            try
            {
                win.Show();
                var result = CaptureEditorElementCommand.CaptureEditorElement(
                    window: nameof(TestCaptureWindow), selector: "#target", output: output);

                Assert.IsTrue(result.Success, result.Message);
                Assert.AreEqual("png", result.Encoding);
                Assert.Greater(result.Width, 0);
                Assert.Greater(result.Height, 0);
                Assert.IsFalse(string.IsNullOrEmpty(result.Base64), "base64 payload should be populated");
                Assert.Greater(result.Bytes, 0);
                Assert.AreEqual(output, result.Path);
                FileAssert.Exists(output);
            }
            finally
            {
                win.Close();
                if (File.Exists(output))
                    File.Delete(output);
            }
        }

        /// <summary>A minimal EditorWindow with one named, sized element to capture.</summary>
        class TestCaptureWindow : EditorWindow
        {
            void CreateGUI()
            {
                var target = new VisualElement
                {
                    name = "target",
                    style =
                    {
                        width = 120,
                        height = 60,
                        backgroundColor = Color.red
                    }
                };
                rootVisualElement.Add(target);
            }
        }
    }
}
#endif
