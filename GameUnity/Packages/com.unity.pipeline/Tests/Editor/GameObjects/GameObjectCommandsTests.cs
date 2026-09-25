using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.Pipeline.Editor.Commands.GameObjects;
using Unity.Pipeline.Models;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using Unity.Pipeline;

namespace Unity.Pipeline.Tests.Editor.GameObjects
{
    /// <summary>
    /// Tests for the GameObject authoring commands (CLI-192), exercised directly and via
    /// <see cref="PipelineTestServer"/>. Covers creation (empty + primitive), hierarchy build via
    /// set_parent, the find query, transform/active/tag/layer/rename mutations, deletion, and the
    /// Undo-revert contract. Test GameObjects are created with plain UnityEngine/UnityEditor APIs and
    /// torn down explicitly so the suite leaves no scene residue.
    /// </summary>
    public class GameObjectCommandsTests
    {
        private readonly List<GameObject> m_Spawned = new List<GameObject>();

        private GameObject Track(GameObject go)
        {
            m_Spawned.Add(go);
            return go;
        }

        private static ObjectRef RefTo(Object obj) => new ObjectRef { InstanceId = PipelineUtils.GetObjectId(obj) };

        [TearDown]
        public void TearDown()
        {
            foreach (var go in m_Spawned)
            {
                if (go != null)
                    Object.DestroyImmediate(go);
            }

            m_Spawned.Clear();
        }

        #region Direct

        [Test]
        public void CreateGameObject_Empty_ReturnsIdentity()
        {
            var result = GameObjectCommands.CreateGameObject("Empty_CLI192");
            var go = PipelineUtils.IdToObject(result.InstanceId.Value) as GameObject;
            Track(go);

            Assert.IsNotNull(go, "Created object should be resolvable by instanceId");
            Assert.AreEqual("Empty_CLI192", go.name);
            Assert.IsNull(go.GetComponent<MeshFilter>(), "Empty GO has no mesh");
            Assert.IsNotNull(result.HierarchyPath, "Scene object should report a hierarchy path");
        }

        [Test]
        public void CreateGameObject_Primitive_AttachesMeshAndCollider()
        {
            var result = GameObjectCommands.CreateGameObject("Cube_CLI192", "cube");
            var go = PipelineUtils.IdToObject(result.InstanceId.Value) as GameObject;
            Track(go);

            Assert.IsNotNull(go.GetComponent<MeshFilter>(), "Primitive cube should have a MeshFilter");
            Assert.IsNotNull(go.GetComponent<BoxCollider>(), "Primitive cube should have a BoxCollider");
        }

        [Test]
        public void CreateGameObject_WithParent_BuildsHierarchy()
        {
            var parent = Track(new GameObject("Parent_CLI192"));
            var childResult = GameObjectCommands.CreateGameObject("Child_CLI192", null, RefTo(parent));
            var child = PipelineUtils.IdToObject(childResult.InstanceId.Value) as GameObject;

            Assert.AreEqual(parent.transform, child.transform.parent, "Child should be parented");
        }

        [Test]
        public void SetTransform_PrimitiveChannels_Apply()
        {
            var go = Track(new GameObject("Xform_CLI192"));

            GameObjectCommands.SetTransform(RefTo(go),
                position: new[] { 1f, 2f, 3f },
                rotation: new[] { 0f, 90f, 0f },
                scale: new[] { 2f, 2f, 2f });

            Assert.AreEqual(new Vector3(1, 2, 3), go.transform.localPosition);
            Assert.AreEqual(new Vector3(2, 2, 2), go.transform.localScale);
            Assert.AreEqual(90f, go.transform.localEulerAngles.y, 0.01f);
        }

        [Test]
        public void SetParent_ToNull_DetachesToRoot()
        {
            var parent = Track(new GameObject("P_CLI192"));
            var child = Track(new GameObject("C_CLI192"));
            child.transform.SetParent(parent.transform);

            GameObjectCommands.SetParent(RefTo(child), null);

            Assert.IsNull(child.transform.parent, "Child should be detached to scene root");
        }

        [Test]
        public void SetActive_TogglesState()
        {
            var go = Track(new GameObject("Active_CLI192"));
            GameObjectCommands.SetActive(RefTo(go), false);
            Assert.IsFalse(go.activeSelf);
            GameObjectCommands.SetActive(RefTo(go), true);
            Assert.IsTrue(go.activeSelf);
        }

        [Test]
        public void SetLayer_ByIndex_Applies()
        {
            var go = Track(new GameObject("Layer_CLI192"));
            GameObjectCommands.SetLayer(RefTo(go), "5"); // UI layer index
            Assert.AreEqual(5, go.layer);
        }

        [Test]
        public void SetTag_UnknownTag_Throws()
        {
            var go = Track(new GameObject("Tag_CLI192"));
            Assert.Throws<System.ArgumentException>(
                () => GameObjectCommands.SetTag(RefTo(go), "__definitely_not_a_real_tag__"));
        }

        [Test]
        public void Rename_ChangesName()
        {
            var go = Track(new GameObject("Old_CLI192"));
            GameObjectCommands.RenameGameObject(RefTo(go), "New_CLI192");
            Assert.AreEqual("New_CLI192", go.name);
        }

        [Test]
        public void FindGameObjects_ByNameAndType_Filters()
        {
            var go = Track(new GameObject("Findable_CLI192"));
            go.AddComponent<Rigidbody>();

            var result = GameObjectCommands.FindGameObjects(name: "Findable_CLI192", type: "Rigidbody");

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(PipelineUtils.GetObjectId(go), result.GameObjects[0].InstanceId);
        }

        [Test]
        public void Delete_RemovesFromScene()
        {
            var go = new GameObject("Doomed_CLI192");
            var id = PipelineUtils.GetObjectId(go);
            GameObjectCommands.DeleteGameObject(new ObjectRef { InstanceId = id });

            Assert.IsNull(PipelineUtils.IdToObject(id), "GameObject should be destroyed");
        }

        [Test]
        public void SetTransform_IsUndone()
        {
            var go = Track(new GameObject("Undo_CLI192"));
            go.transform.localPosition = Vector3.zero;

            GameObjectCommands.SetTransform(RefTo(go), position: new[] { 5f, 5f, 5f });
            Assert.AreEqual(new Vector3(5, 5, 5), go.transform.localPosition);

            Undo.PerformUndo();
            Assert.AreEqual(Vector3.zero, go.transform.localPosition, "Undo should revert the transform change");
        }

        #endregion

        #region ViaClient

        [Test]
        public void CreateGameObject_ViaClient_Succeeds()
        {
            using (var server = new PipelineTestServer())
            {
                var response = server.Execute("create_gameobject", new { name = "ViaClient_CLI192", primitive = "sphere" });

                Assert.IsTrue(response.IsSuccess, $"create_gameobject should succeed: {response.Error}");
                Assert.IsTrue(response.HasValidJson, "Response should have valid JSON");
                Assert.IsTrue(response.JsonResponse.ContainsKey("result"), "Should have result field");

                var instanceId = response.JsonResponse["result"]?["instanceId"]?.ToObject<ObjectId?>();
                Assert.IsTrue(instanceId.HasValue, "Result should carry an instanceId");
                var go = PipelineUtils.IdToObject(instanceId.Value) as GameObject;
                Track(go);
                Assert.IsNotNull(go, "Created object should be resolvable");
                Assert.IsNotNull(go.GetComponent<SphereCollider>(), "Sphere primitive should have a SphereCollider");
            }
        }

        [Test]
        public void FindGameObjects_ViaClient_ReturnsMatches()
        {
            var go = Track(new GameObject("ViaFind_CLI192"));

            using (var server = new PipelineTestServer())
            {
                var response = server.Execute("find_gameobjects", new { name = "ViaFind_CLI192" });

                Assert.IsTrue(response.IsSuccess, $"find_gameobjects should succeed: {response.Error}");
                var count = response.JsonResponse["result"]?["count"]?.ToObject<int>();
                Assert.AreEqual(1, count, "Should find exactly the one named GameObject");
            }
        }

        #endregion
    }
}
