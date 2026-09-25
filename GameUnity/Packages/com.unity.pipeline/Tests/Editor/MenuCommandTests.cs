using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor;
using Unity.Pipeline.Editor.Commands;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Pipeline.Tests.Editor
{
    /// <summary>
    /// Tests for the <c>menu</c> command (CLI-110). Uses a test-only menu item so the positive path
    /// is deterministic and free of side effects, rather than relying on built-in Editor menus.
    /// </summary>
    public class MenuCommandTests
    {
        const string k_TestMenuPath = "PipelineTests/Invoke Marker";
        static bool s_MarkerInvoked;

        [MenuItem(k_TestMenuPath)]
        static void InvokeMarker() => s_MarkerInvoked = true;

        [SetUp]
        public void SetUp()
        {
            CommandRegistry.SetDiscovery(new TypeCacheCommandDiscovery());
            s_MarkerInvoked = false;
        }

        [Test]
        public void Menu_IsDiscovered_WithExpectedSchema()
        {
            var cmd = CommandRegistry.DiscoverCommands().FirstOrDefault(c => c.Name == "menu");

            Assert.IsNotNull(cmd, "Should discover the menu command");
            Assert.IsTrue(cmd.MainThreadRequired, "menu calls Editor APIs and must run on the main thread");
            Assert.AreEqual(1, cmd.Parameters.Count, "menu should take a single 'path' parameter");

            var p = cmd.Parameters[0];
            Assert.AreEqual("path", p.Name);
            Assert.IsFalse(p.Required, "path should be optional (omitting it lists available items)");
        }

        [Test]
        public void Menu_NoPath_ListsAvailableItems()
        {
            var result = MenuItemCommand.ExecuteMenu("");

            Assert.IsTrue(result.Success, "Listing should succeed");
            Assert.IsNotNull(result.Items, "Items should be populated in list mode");
            Assert.Greater(result.Items.Count, 0, "There should be at least one discovered menu item");
            // Our test-only [MenuItem] is declared via attribute, so it must be discoverable.
            CollectionAssert.Contains(result.Items, k_TestMenuPath,
                "The test menu item should appear in the discovered list");
            Assert.IsFalse(s_MarkerInvoked, "Listing must not execute any menu item");
        }

        [Test]
        public void Menu_NoPath_IncludesNativeMenuItems()
        {
            // The whole point of using Menu.GetMenuItems instead of a [MenuItem] TypeCache scan is to
            // also surface menus Unity defines natively in C++ — e.g. the Assets menu built by
            // AssetsMenu.cpp:Build. Those entries carry no [MenuItem] attribute, so this test proves
            // the listing includes a native item the attribute-only approach would have missed.
            var items = MenuItemCommand.ExecuteMenu("").Items;
            Assert.IsNotNull(items);

            // What the old attribute-only approach saw (trailing keyboard shortcuts stripped, so the
            // paths compare against the live, shortcut-free menu paths).
            var attributed = new HashSet<string>(
                TypeCache.GetMethodsWithAttribute<MenuItem>()
                    .SelectMany(m => m.GetCustomAttributes(typeof(MenuItem), false).Cast<MenuItem>())
                    .Where(a => !a.validate && !string.IsNullOrEmpty(a.menuItem))
                    .Select(a => StripShortcut(a.menuItem)));

            var nativeAssetsItem = items.FirstOrDefault(
                p => p.StartsWith("Assets/Create/") && !attributed.Contains(p));

            Assert.IsNotNull(nativeAssetsItem,
                "Listing should include a natively-defined Assets/Create item not declared by any [MenuItem]");
        }

        // Strip a trailing keyboard-shortcut token (e.g. "Edit/Undo %z" -> "Edit/Undo") so attributed
        // paths line up with the shortcut-free paths Menu.GetMenuItems returns.
        static string StripShortcut(string menuItem)
        {
            var i = menuItem.LastIndexOf(' ');
            if (i > 0 && i < menuItem.Length - 1 && "%#&_".IndexOf(menuItem[i + 1]) >= 0)
                return menuItem.Substring(0, i).TrimEnd();
            return menuItem;
        }

        [Test]
        public void Menu_ValidItem_ExecutesAndReportsSuccess()
        {
            Assert.IsFalse(s_MarkerInvoked, "Precondition: marker not yet invoked");

            var result = MenuItemCommand.ExecuteMenu(k_TestMenuPath);

            Assert.IsTrue(result.Success, $"Expected success. Message: {result.Message}");
            Assert.AreEqual(k_TestMenuPath, result.Path);
            Assert.IsTrue(s_MarkerInvoked, "The menu item's action should have run");
        }

        [Test]
        public void Menu_UnknownItem_ReturnsFailure()
        {
            // ExecuteMenuItem logs a Unity error for a missing menu; expect it so the test framework
            // does not treat that expected log as a failure.
            LogAssert.Expect(LogType.Error, new Regex("ExecuteMenuItem failed because there is no menu named"));

            var result = MenuItemCommand.ExecuteMenu("PipelineTests/This Item Does Not Exist 12345");

            Assert.IsFalse(result.Success, "An unknown menu item should fail");
            StringAssert.Contains("not found", result.Message);
            Assert.IsFalse(s_MarkerInvoked, "No action should have run");
        }

        [Test]
        public void Menu_ViaClient_ExecutesItem()
        {
            // Use the isolated PipelineTestServer (not the live editor server) so running the suite
            // never disturbs the server agents drive over HTTP. Mirrors CodeEvalCommandTests.
            using (var server = new PipelineTestServer())
            {
                var response = server.Execute("menu", new { path = k_TestMenuPath });
                Assert.IsTrue(response.IsSuccess, response.Error);

                var result = response.GetTypedResponse<MenuResponse>();
                Assert.IsNotNull(result, "Should deserialize a MenuResponse");
                Assert.IsTrue(result.Success, result.Message);
                Assert.AreEqual(k_TestMenuPath, result.Path);
                Assert.IsTrue(s_MarkerInvoked, "The menu item's action should have run via HTTP");
            }
        }
    }
}
