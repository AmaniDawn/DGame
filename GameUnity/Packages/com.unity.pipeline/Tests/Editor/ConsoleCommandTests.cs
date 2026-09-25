using System;
using System.Linq;
using NUnit.Framework;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Console;
using Unity.Pipeline.Editor;
using Unity.Pipeline.Runtime.Commands;
using UnityEngine;

namespace Unity.Pipeline.Tests.Editor
{
    /// <summary>
    /// Tests for the console command (<see cref="ConsoleCommand"/>).
    ///
    /// Entries are injected straight into the shared buffer via <see cref="ConsoleLogBuffer.Add"/>
    /// (rather than real Debug.Log calls) so the tests are deterministic and don't trip Unity's
    /// LogAssert on error-level logs. Each test tags its entries with a unique marker and filters on
    /// it, so unrelated editor logs captured concurrently don't affect assertions.
    /// </summary>
    public class ConsoleCommandTests
    {
        string m_Marker;

        [SetUp]
        public void SetUp()
        {
            ConsoleLogCapture.Buffer.Clear();
            m_Marker = $"[ectest-{Guid.NewGuid():N}] ";
        }

        string[] MineInOrder(Unity.Pipeline.Models.ConsoleLogResponse response)
        {
            return response.Entries
                .Where(e => e.Message != null && e.Message.StartsWith(m_Marker))
                .Select(e => e.Message.Substring(m_Marker.Length))
                .ToArray();
        }

        void Add(LogType type, string message) =>
            ConsoleLogCapture.Buffer.Add(type, m_Marker + message, null, DateTime.UtcNow);

        [Test]
        public void GetConsole_Default_ReturnsRecentEntriesInOrder()
        {
            Add(LogType.Log, "one");
            Add(LogType.Log, "two");
            Add(LogType.Log, "three");

            var response = ConsoleCommand.GetConsole();

            CollectionAssert.AreEqual(new[] { "one", "two", "three" }, MineInOrder(response));
        }

        [Test]
        public void GetConsole_LevelError_ReturnsOnlyErrorSeverity()
        {
            Add(LogType.Log, "plain");
            Add(LogType.Warning, "careful");
            Add(LogType.Error, "broken");
            Add(LogType.Exception, "threw");

            var response = ConsoleCommand.GetConsole(level: "error");

            CollectionAssert.AreEqual(new[] { "broken", "threw" }, MineInOrder(response));
        }

        [Test]
        public void GetConsole_LevelWarn_IncludesWarningsAndErrors()
        {
            Add(LogType.Log, "plain");
            Add(LogType.Warning, "careful");
            Add(LogType.Error, "broken");

            var response = ConsoleCommand.GetConsole(level: "warn");

            CollectionAssert.AreEqual(new[] { "careful", "broken" }, MineInOrder(response));
        }

        [Test]
        public void GetConsole_Since_ReturnsOnlyNewerEntries()
        {
            Add(LogType.Log, "before");
            long cursor = ConsoleCommand.GetConsole().Cursor;
            Add(LogType.Log, "after");

            var response = ConsoleCommand.GetConsole(since: cursor);

            CollectionAssert.AreEqual(new[] { "after" }, MineInOrder(response));
        }

        [Test]
        public void GetConsole_TailZero_IsCoercedToDefault()
        {
            Add(LogType.Log, "a");
            Add(LogType.Log, "b");

            // tail <= 0 must not mean "unlimited"; it falls back to the default and still returns ours.
            var response = ConsoleCommand.GetConsole(tail: 0);

            Assert.LessOrEqual(response.Returned, ConsoleCommand.DefaultTail);
            CollectionAssert.AreEqual(new[] { "a", "b" }, MineInOrder(response));
        }

        [Test]
        public void Console_IsDiscoverableWithExpectedArgs()
        {
            CommandRegistry.SetDiscovery(new TypeCacheCommandDiscovery());
            var command = CommandRegistry.DiscoverCommands().FirstOrDefault(c => c.Name == "console");

            Assert.IsNotNull(command, "console should be discoverable");
            Assert.IsFalse(command.MainThreadRequired, "console reads the buffer only; no main thread needed");

            var paramNames = command.Parameters.Select(p => p.Name).ToList();
            CollectionAssert.Contains(paramNames, "tail");
            CollectionAssert.Contains(paramNames, "level");
            CollectionAssert.Contains(paramNames, "since");
        }
    }
}
