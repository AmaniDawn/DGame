using System.Collections.Generic;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using Unity.Pipeline.Editor.Commands.GameObjects;
using Unity.Pipeline.Models;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using Unity.Pipeline;

namespace Unity.Pipeline.Tests.Editor.GameObjects
{
    /// <summary>
    /// Tests for the shared string-encoded JSON parameter coercion (CLI-219 / CLI-220) and the batch
    /// <c>create_gameobjects</c> command (CLI-223).
    ///
    /// CLI-219/220 are a single root cause: when an agent/CLI passes a structured parameter as a
    /// JSON-ENCODED STRING (position "[1,2,3]", properties "{\"m_Mass\":0.17}"), the server must
    /// re-parse it before converting. The ViaClient tests below pass those forms as real strings to
    /// prove the coercion; the Direct tests pass real typed values to prove the typed path still works.
    /// The guard is also pinned: a plain string param and an ObjectRef string handle ("/Name") must NOT
    /// be re-parsed.
    /// </summary>
    public class CoercionAndBatchTests
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

        #region CLI-219 set_transform

        [Test]
        public void SetTransform_Direct_TypedArrays_Apply()
        {
            var go = Track(new GameObject("Xform219_Direct"));

            GameObjectCommands.SetTransform(RefTo(go),
                position: new[] { 1f, 2f, 3f },
                rotation: new[] { 0f, 90f, 0f },
                scale: new[] { 2f, 2f, 2f });

            Assert.AreEqual(new Vector3(1, 2, 3), go.transform.localPosition);
            Assert.AreEqual(new Vector3(2, 2, 2), go.transform.localScale);
            Assert.AreEqual(90f, go.transform.localEulerAngles.y, 0.01f);
        }

        [Test]
        public void SetTransform_ViaClient_ArrayParams_Apply()
        {
            var go = Track(new GameObject("Xform219_Arrays"));

            using (var server = new PipelineTestServer())
            {
                var response = server.Execute("set_transform", new
                {
                    target = new { instanceId = PipelineUtils.GetObjectId(go) },
                    position = new[] { 1f, 2f, 3f },
                    rotation = new[] { 0f, 90f, 0f },
                    scale = new[] { 2f, 2f, 2f }
                });

                Assert.IsTrue(response.IsSuccess, $"set_transform should succeed: {response.Error}");
                Assert.AreEqual(new Vector3(1, 2, 3), go.transform.localPosition);
                Assert.AreEqual(new Vector3(2, 2, 2), go.transform.localScale);
                Assert.AreEqual(90f, go.transform.localEulerAngles.y, 0.01f);
            }
        }

        [Test]
        public void SetTransform_ViaClient_JsonStringParams_Coerced()
        {
            // The CLI-219 repro: position/rotation/scale arrive as JSON-encoded STRINGS. Before the
            // coercion these convert to null (JValue(string).ToObject(float[]) == null) and the command
            // silently applies nothing. They must now be re-parsed and applied.
            var go = Track(new GameObject("Xform219_Strings"));

            using (var server = new PipelineTestServer())
            {
                var response = server.Execute("set_transform", new
                {
                    target = new { instanceId = PipelineUtils.GetObjectId(go) },
                    position = "[1,2,3]",
                    rotation = "[0,90,0]",
                    scale = "[2,2,2]"
                });

                Assert.IsTrue(response.IsSuccess, $"set_transform should succeed: {response.Error}");
                Assert.AreEqual(new Vector3(1, 2, 3), go.transform.localPosition,
                    "String-encoded position must be coerced and applied");
                Assert.AreEqual(new Vector3(2, 2, 2), go.transform.localScale,
                    "String-encoded scale must be coerced and applied");
                Assert.AreEqual(90f, go.transform.localEulerAngles.y, 0.01f,
                    "String-encoded rotation must be coerced and applied");
            }
        }

        [Test]
        public void SetTransform_ViaClient_NestedTarget_StringParams_Coerced()
        {
            // Cover a parented/nested target too: the handle resolves the child, and the string-encoded
            // position applies as a LOCAL transform on the child.
            var parent = Track(new GameObject("Xform219_Parent"));
            var child = Track(new GameObject("Xform219_Child"));
            child.transform.SetParent(parent.transform);

            using (var server = new PipelineTestServer())
            {
                var response = server.Execute("set_transform", new
                {
                    target = new { instanceId = PipelineUtils.GetObjectId(child) },
                    position = "[4,5,6]"
                });

                Assert.IsTrue(response.IsSuccess, $"set_transform should succeed: {response.Error}");
                Assert.AreEqual(parent.transform, child.transform.parent, "Child stays parented");
                Assert.AreEqual(new Vector3(4, 5, 6), child.transform.localPosition,
                    "String-encoded local position must apply to the nested child");
            }
        }

        #endregion

        #region CLI-220 set_component_properties

        [Test]
        public void SetComponentProperties_Direct_JObject_Applies()
        {
            var go = Track(new GameObject("Props220_Direct"));
            var rb = go.AddComponent<Rigidbody>();

            ComponentCommands.SetComponentProperties(RefTo(rb), new JObject { ["m_Mass"] = 0.17f });

            Assert.AreEqual(0.17f, rb.mass, 0.0001f, "Real JObject properties must still apply");
        }

        [Test]
        public void SetComponentProperties_ViaClient_PropertiesAsJsonString_Coerced()
        {
            // The CLI-220 repro: `properties` arrives as a JSON-encoded STRING. Before the coercion it
            // converts to null and the command fails "Required parameter 'properties' is missing or
            // empty". It must now be re-parsed into a JObject and applied.
            var go = Track(new GameObject("Props220_String"));
            var rb = go.AddComponent<Rigidbody>();

            using (var server = new PipelineTestServer())
            {
                var response = server.Execute("set_component_properties", new
                {
                    target = new { instanceId = PipelineUtils.GetObjectId(rb) },
                    type = "Rigidbody",
                    properties = "{\"m_Mass\":0.17}"
                });

                Assert.IsTrue(response.IsSuccess,
                    $"set_component_properties should succeed with a string-encoded properties: {response.Error}");
                Assert.AreEqual(0.17f, rb.mass, 0.0001f,
                    "String-encoded properties must be coerced and applied");
            }
        }

        #endregion

        #region CLI-223 create_gameobjects (batch)

        [Test]
        public void CreateGameObjects_Direct_CountOne_ReturnsSingleIdentity()
        {
            // Regression: count=1 keeps the bare name (no "1" suffix) and returns exactly one item.
            var result = GameObjectCommands.CreateGameObjects(name: "Batch223_One", primitive: "cube", count: 1);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(1, result.GameObjects.Count);

            var go = PipelineUtils.IdToObject(result.GameObjects[0].InstanceId.Value) as GameObject;
            Track(go);
            Assert.IsNotNull(go, "Created object should be resolvable by instanceId");
            Assert.AreEqual("Batch223_One", go.name, "count=1 must not append a numeric suffix");
            Assert.IsNotNull(go.GetComponent<BoxCollider>(), "Cube primitive should have a BoxCollider");
        }

        [Test]
        public void CreateGameObjects_Direct_CountN_WithPositions_AppliesEach()
        {
            var positions = new[]
            {
                new[] { 0f, 0f, 0f },
                new[] { 1f, 0f, 0f },
                new[] { 2f, 0f, 0f },
            };

            var result = GameObjectCommands.CreateGameObjects(
                name: "Batch223_Pos", primitive: "sphere", count: 3, positions: positions);

            Assert.AreEqual(3, result.Count);
            for (int i = 0; i < 3; i++)
            {
                var go = PipelineUtils.IdToObject(result.GameObjects[i].InstanceId.Value) as GameObject;
                Track(go);
                Assert.AreEqual("Batch223_Pos" + (i + 1), go.name, "count>1 must suffix Name1..NameN");
                Assert.AreEqual(new Vector3(i, 0, 0), go.transform.localPosition,
                    "Each object must take its per-index position");
            }
        }

        [Test]
        public void CreateGameObjects_Direct_CountN_NoPositions_StackedAtOrigin()
        {
            var result = GameObjectCommands.CreateGameObjects(name: "Batch223_Origin", count: 4);

            Assert.AreEqual(4, result.Count);
            foreach (var item in result.GameObjects)
            {
                var go = PipelineUtils.IdToObject(item.InstanceId.Value) as GameObject;
                Track(go);
                Assert.AreEqual(Vector3.zero, go.transform.localPosition,
                    "With no positions array, all objects stay at the origin");
            }
        }

        [Test]
        public void CreateGameObjects_Direct_MismatchedPositionsLength_Throws()
        {
            // 2 vectors for count=3 is an agent error and must reject before creating anything.
            var positions = new[]
            {
                new[] { 0f, 0f, 0f },
                new[] { 1f, 0f, 0f },
            };

            Assert.Throws<System.ArgumentException>(() =>
                GameObjectCommands.CreateGameObjects(name: "Batch223_Bad", count: 3, positions: positions));
        }

        [Test]
        public void CreateGameObjects_Direct_NullPositionEntry_ThrowsWithIndex()
        {
            // A positions array whose LENGTH matches count but contains a null entry must fail with a
            // clear, index-bearing ArgumentException (not a NullReferenceException), up front, before any
            // object is created.
            var positions = new[]
            {
                new[] { 0f, 0f, 0f },
                null,
                new[] { 2f, 0f, 0f },
            };

            var ex = Assert.Throws<System.ArgumentException>(() =>
                GameObjectCommands.CreateGameObjects(name: "Batch223_NullEntry", count: 3, positions: positions));
            StringAssert.Contains("positions[1]", ex.Message, "The error should name the offending index");

            // Validation runs before creation, so nothing should have been spawned.
            var leaked = GameObjectCommands.FindGameObjects(name: "Batch223_NullEntry1");
            Assert.AreEqual(0, leaked.Count, "A rejected batch must create nothing");
        }

        [Test]
        public void CreateGameObjects_ViaClient_CountN_WithStringEncodedPositions()
        {
            // End-to-end: float[][] positions arrive as a JSON STRING (the CLI-219/220 coercion also
            // re-parses string-encoded nested arrays), and each object takes its per-index position.
            using (var server = new PipelineTestServer())
            {
                var response = server.Execute("create_gameobjects", new
                {
                    name = "Batch223_Via",
                    primitive = "cube",
                    count = 3,
                    positions = "[[0,0,0],[1,0,0],[2,0,0]]"
                });

                Assert.IsTrue(response.IsSuccess, $"create_gameobjects should succeed: {response.Error}");

                var count = response.JsonResponse["result"]?["count"]?.ToObject<int>();
                Assert.AreEqual(3, count, "Batch should report 3 created");

                var gameObjects = response.JsonResponse["result"]?["gameObjects"] as JArray;
                Assert.IsNotNull(gameObjects, "Result should carry a gameObjects array");
                Assert.AreEqual(3, gameObjects.Count);

                for (int i = 0; i < 3; i++)
                {
                    var instanceId = gameObjects[i]?["instanceId"]?.ToObject<ObjectId?>();
                    Assert.IsTrue(instanceId.HasValue, "Each created object should carry an instanceId");
                    var go = PipelineUtils.IdToObject(instanceId.Value) as GameObject;
                    Track(go);
                    Assert.IsNotNull(go, "Created object should be resolvable");
                    Assert.AreEqual(new Vector3(i, 0, 0), go.transform.localPosition,
                        "Each object must take its per-index position (string-encoded array coerced)");
                }
            }
        }

        [Test]
        public void CreateGameObjects_ViaClient_CountOne_Regression()
        {
            using (var server = new PipelineTestServer())
            {
                var response = server.Execute("create_gameobjects", new { name = "Batch223_ViaOne", count = 1 });

                Assert.IsTrue(response.IsSuccess, $"create_gameobjects should succeed: {response.Error}");
                var count = response.JsonResponse["result"]?["count"]?.ToObject<int>();
                Assert.AreEqual(1, count);

                var instanceId = response.JsonResponse["result"]?["gameObjects"]?[0]?["instanceId"]?.ToObject<ObjectId?>();
                Assert.IsTrue(instanceId.HasValue);
                var go = PipelineUtils.IdToObject(instanceId.Value) as GameObject;
                Track(go);
                Assert.AreEqual("Batch223_ViaOne", go.name, "count=1 must not append a numeric suffix");
            }
        }

        #endregion

        #region Coercion guard (does not break plain strings / ObjectRef handles)

        [Test]
        public void Coercion_PlainStringParam_NotReparsed()
        {
            // rename_gameobject takes a plain string 'name'. A normal string must pass straight through
            // (it does not start with '{' or '[', so the guard never touches it).
            var go = Track(new GameObject("Guard_Old"));

            using (var server = new PipelineTestServer())
            {
                var response = server.Execute("rename_gameobject", new
                {
                    target = new { instanceId = PipelineUtils.GetObjectId(go) },
                    name = "Guard_New"
                });

                Assert.IsTrue(response.IsSuccess, $"rename_gameobject should succeed: {response.Error}");
                Assert.AreEqual("Guard_New", go.name, "Plain string param must apply unchanged");
            }
        }

        [Test]
        public void Coercion_ObjectRefStringHandle_StillResolves()
        {
            // An ObjectRef passed as a bare string handle ("/Name") must continue flowing to
            // ObjectRefConverter — it does not start with '{' or '[', so the coercion leaves it alone.
            var go = Track(new GameObject("Guard_HandleTarget"));

            using (var server = new PipelineTestServer())
            {
                var response = server.Execute("set_active", new
                {
                    target = "/Guard_HandleTarget",
                    active = false
                });

                Assert.IsTrue(response.IsSuccess,
                    $"set_active with a string handle should succeed: {response.Error}");
                Assert.IsFalse(go.activeSelf, "ObjectRef string handle must still resolve and apply");
            }
        }

        #endregion
    }
}
