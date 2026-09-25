using System;
using System.Linq;
using NUnit.Framework;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor;
using Unity.Pipeline.Editor.Commands;

namespace Unity.Pipeline.Tests.Editor
{
    /// <summary>
    /// Tests for the recompile command's optional editor-focus behaviour: focus is off by default and
    /// only performed when focus=true. The focus action is indirected via
    /// <see cref="RecompileCommand.s_FocusAction"/> so these tests observe whether it ran without
    /// stealing the OS foreground window (or triggering a real domain reload).
    /// </summary>
    public class RecompileCommandTests
    {
        Action m_OriginalFocusAction;

        [SetUp]
        public void SetUp()
        {
            CommandRegistry.SetDiscovery(new TypeCacheCommandDiscovery());
            m_OriginalFocusAction = RecompileCommand.s_FocusAction;
        }

        [TearDown]
        public void TearDown()
        {
            RecompileCommand.s_FocusAction = m_OriginalFocusAction;
        }

        [Test]
        public void Recompile_IsDiscovered_WithOptionalFocusParam()
        {
            var commands = CommandRegistry.DiscoverCommands().ToList();

            var recompile = commands.FirstOrDefault(c => c.Name == "recompile");
            Assert.IsNotNull(recompile, "Should discover recompile");
            CollectionAssert.AreEquivalent(
                new[] { "focus" },
                recompile.Parameters.Select(p => p.Name).ToList(),
                "recompile should expose exactly the optional 'focus' parameter");
            Assert.IsFalse(recompile.Parameters.Single(p => p.Name == "focus").Required,
                "'focus' must be optional");

            var status = commands.FirstOrDefault(c => c.Name == "recompile_status");
            Assert.IsNotNull(status, "Should discover recompile_status");
            Assert.AreEqual(0, status.Parameters.Count, "recompile_status takes no parameters");
        }

        [Test]
        public void Recompile_ByDefault_DoesNotFocusEditor()
        {
            var focusCount = 0;
            RecompileCommand.s_FocusAction = () => focusCount++;

            RecompileCommand.Recompile();

            Assert.AreEqual(0, focusCount, "recompile must not focus the editor by default");
        }

        [Test]
        public void Recompile_WithFocusTrue_FocusesEditor()
        {
            var focusCount = 0;
            RecompileCommand.s_FocusAction = () => focusCount++;

            RecompileCommand.Recompile(focus: true);

            Assert.AreEqual(1, focusCount, "recompile with focus=true must focus the editor once");
        }
    }
}
