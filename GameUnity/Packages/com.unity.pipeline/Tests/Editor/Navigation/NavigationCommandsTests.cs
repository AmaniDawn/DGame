using System.Collections.Generic;
using NUnit.Framework;
using Unity.Pipeline.Editor.Commands.Navigation;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using Unity.Pipeline;

namespace Unity.Pipeline.Tests.Editor.Navigation
{
    /// <summary>
    /// Tests for the navigation/targeting commands (CLI-200): get_selection, set_selection, and
    /// search, exercised directly and via PipelineClient. Mirrors FolderCommandsTests for the
    /// ViaClient style. Project contents are unknown, so search assertions check structural success
    /// rather than a specific count.
    /// </summary>
    public class NavigationCommandsTests
    {
        private readonly List<GameObject> m_Temp = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            Selection.objects = new Object[0];
            foreach (var go in m_Temp)
            {
                if (go != null)
                    Object.DestroyImmediate(go);
            }
            m_Temp.Clear();
        }

        private GameObject NewTempObject(string name)
        {
            var go = new GameObject(name);
            m_Temp.Add(go);
            return go;
        }

        #region Direct

        [Test]
        public void GetSelection_ReflectsActiveObject()
        {
            var go = NewTempObject("CLI200_GetSelection");
            Selection.activeObject = go;

            var result = NavigationCommands.GetSelection();

            Assert.IsNotNull(result);
            Assert.GreaterOrEqual(result.Count, 1);
            Assert.IsNotNull(result.Active, "Active object should be described");
            Assert.AreEqual(PipelineUtils.GetObjectId(go), result.Active.InstanceId);
        }

        [Test]
        public void SetSelection_ByInstanceId_SelectsObject()
        {
            var go = NewTempObject("CLI200_SetSelection");

            var result = NavigationCommands.SetSelection(instanceIds: new[] { PipelineUtils.GetObjectId(go) });

            Assert.AreSame(go, Selection.activeObject, "Selection.activeObject should be the requested object");
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(PipelineUtils.GetObjectId(go), result.Active.InstanceId);
        }

        [Test]
        public void SetSelection_ByPath_SelectsAsset()
        {
            // "Assets" is always present and loads as a DefaultAsset, so this needs no project content.
            var result = NavigationCommands.SetSelection(paths: new[] { "Assets" });

            Assert.AreSame(
                AssetDatabase.LoadMainAssetAtPath("Assets"),
                Selection.activeObject,
                "Selection.activeObject should be the asset loaded from the requested path");
            Assert.AreEqual(1, result.Count);
        }

        [Test]
        public void SetSelection_NoInputs_ClearsSelection()
        {
            var go = NewTempObject("CLI200_ClearSelection");
            Selection.activeObject = go;

            var result = NavigationCommands.SetSelection();

            Assert.AreEqual(0, result.Count);
            Assert.IsNull(Selection.activeObject, "Empty inputs should clear the selection");
        }

        [Test]
        public void SetSelection_UnresolvedInput_Throws()
        {
            // 0 is never a valid instance id, and an empty list of resolved objects with inputs
            // present is a hard error rather than a silent clear.
            Assert.Throws<System.ArgumentException>(
                () => NavigationCommands.SetSelection(instanceIds: new[] { ObjectId.FromRaw(0) }));
        }

        [Test]
        public void Search_DefaultAsset_ReturnsStructuredResult()
        {
            // "t:DefaultAsset" is guaranteed not to throw regardless of project contents.
            var result = NavigationCommands.Search("t:DefaultAsset", 10);

            Assert.IsNotNull(result);
            Assert.AreEqual("t:DefaultAsset", result.Query);
            Assert.GreaterOrEqual(result.Count, 0);
            Assert.IsNotNull(result.Results);
            Assert.LessOrEqual(result.Count, 10, "Result count should respect the limit");
        }

        #endregion

        #region ViaClient

        [Test]
        public void GetSelection_ViaClient_Succeeds()
        {
            using (var server = new PipelineTestServer())
            {
                var response = server.Execute("get_selection");

                Assert.IsTrue(response.IsSuccess, $"get_selection should succeed: {response.Error}");
                Assert.IsTrue(response.HasValidJson, "Response should have valid JSON");
                Assert.IsTrue(response.JsonResponse.ContainsKey("result"), "Should have result field");
            }
        }

        [Test]
        public void Search_ViaClient_Succeeds()
        {
            using (var server = new PipelineTestServer())
            {
                var response = server.Execute("search", new { query = "t:DefaultAsset", limit = 5 });

                Assert.IsTrue(response.IsSuccess, $"search should succeed: {response.Error}");
                Assert.IsTrue(response.HasValidJson, "Response should have valid JSON");
                Assert.IsTrue(response.JsonResponse.ContainsKey("result"), "Should have result field");
            }
        }

        #endregion
    }
}
