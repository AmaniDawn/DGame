using NUnit.Framework;
using Newtonsoft.Json;
using Unity.Pipeline;
using Unity.Pipeline.Models;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.Pipeline.Tests.Editor.Authoring
{
    /// <summary>
    /// Tests for <see cref="ObjectRefConverter"/>: a single canonical string handle coerces into the
    /// matching ObjectRef field (mirroring ObjectResolver/ToString priority), structured objects still
    /// deserialize fully, and serialization still emits a JSON object. Plus one end-to-end ViaClient
    /// test proving a string target flows through the real /api/exec dispatch path.
    /// </summary>
    public class ObjectRefConverterTests
    {
        private static ObjectRef Parse(string handle) =>
            JsonConvert.DeserializeObject<ObjectRef>("\"" + handle + "\"");

        #region String coercion (one field per canonical form)

        [Test]
        public void String_HierarchyPath_LeadingSlash()
        {
            var r = Parse("/Player");
            Assert.AreEqual("/Player", r.HierarchyPath);
            Assert.IsNull(r.GlobalId); Assert.IsNull(r.Path); Assert.IsNull(r.Guid);
            Assert.IsNull(r.InstanceId); Assert.IsNull(r.FileId);
        }

        [Test]
        public void String_HierarchyPath_Nested()
        {
            Assert.AreEqual("/Level/Floor", Parse("/Level/Floor").HierarchyPath);
        }

        [Test]
        public void String_GlobalId()
        {
            var r = Parse("GlobalObjectId_V1-2-abc123def4560000abc123def4560000-1095335325-0");
            Assert.AreEqual("GlobalObjectId_V1-2-abc123def4560000abc123def4560000-1095335325-0", r.GlobalId);
            Assert.IsNull(r.HierarchyPath);
        }

        [Test]
        public void String_AssetPath()
        {
            var r = Parse("Assets/Prefabs/Enemy.prefab");
            Assert.AreEqual("Assets/Prefabs/Enemy.prefab", r.Path);
            Assert.IsNull(r.HierarchyPath);
        }

        [Test]
        public void String_GuidWithFileId()
        {
            var r = Parse("guid:a1b2c3d4e5f6:123");
            Assert.AreEqual("a1b2c3d4e5f6", r.Guid);
            Assert.AreEqual(123L, r.FileId);
        }

        [Test]
        public void String_GuidOnly_Prefixed()
        {
            var r = Parse("guid:a1b2c3d4e5f6");
            Assert.AreEqual("a1b2c3d4e5f6", r.Guid);
            Assert.IsNull(r.FileId);
        }

        [Test]
        public void String_InstanceId_Prefixed_Negative()
        {
#if !UNITY_6000_4_OR_NEWER
            Assert.AreEqual(ObjectId.FromRaw(-3070), Parse("instanceId:-3070").InstanceId);
#else
            Assert.Ignore("Negative int instance ids do not exist in the EntityId (ulong) model on 6000.4+.");
#endif
        }

        [Test]
        public void String_PlainNegativeInt_IsInstanceId()
        {
#if !UNITY_6000_4_OR_NEWER
            var r = Parse("-3070");
            Assert.AreEqual(ObjectId.FromRaw(-3070), r.InstanceId);
            Assert.IsNull(r.HierarchyPath);
#else
            Assert.Ignore("Negative int instance ids do not exist in the EntityId (ulong) model on 6000.4+.");
#endif
        }

        [Test]
        public void String_PlainPositiveInt_IsInstanceId()
        {
            Assert.AreEqual(ObjectId.FromRaw(48184), Parse("48184").InstanceId);
        }

        [Test]
        public void String_32Hex_IsGuid()
        {
            var r = Parse("a1b2c3d4e5f60718293a4b5c6d7e8f90");
            Assert.AreEqual("a1b2c3d4e5f60718293a4b5c6d7e8f90", r.Guid);
            Assert.IsNull(r.InstanceId);
        }

        [Test]
        public void String_BareName_FallsBackToHierarchyPath()
        {
            var r = Parse("Enemy_1");
            Assert.AreEqual("Enemy_1", r.HierarchyPath);
            Assert.IsNull(r.InstanceId); Assert.IsNull(r.Guid);
        }

        #endregion

        #region Object form, write, null

        [Test]
        public void Object_Structured_DeserializesAllFields()
        {
            var json = "{\"globalId\":\"G\",\"path\":\"Assets/A\",\"guid\":\"a1\",\"fileId\":42,\"instanceId\":7,\"hierarchyPath\":\"/H\"}";
            var r = JsonConvert.DeserializeObject<ObjectRef>(json);
            Assert.AreEqual("G", r.GlobalId);
            Assert.AreEqual("Assets/A", r.Path);
            Assert.AreEqual("a1", r.Guid);
            Assert.AreEqual(42L, r.FileId);
            Assert.AreEqual(ObjectId.FromRaw(7), r.InstanceId);
            Assert.AreEqual("/H", r.HierarchyPath);
        }

        [Test]
        public void WriteJson_EmitsObject_NotString_AndRoundTrips()
        {
            var json = JsonConvert.SerializeObject(new ObjectRef { HierarchyPath = "/Player" });
            StringAssert.StartsWith("{", json, "ObjectRef must serialize as a JSON object, not a bare string");
            StringAssert.Contains("\"hierarchyPath\":\"/Player\"", json);

            var back = JsonConvert.DeserializeObject<ObjectRef>(json);
            Assert.AreEqual("/Player", back.HierarchyPath);
        }

        [Test]
        public void Null_DeserializesToNull()
        {
            Assert.IsNull(JsonConvert.DeserializeObject<ObjectRef>("null"));
        }

        [Test]
        public void Number_BareInteger_IsInstanceId()
        {
            Assert.AreEqual(ObjectId.FromRaw(48184), JsonConvert.DeserializeObject<ObjectRef>("48184").InstanceId);
        }

        #endregion

        #region End-to-end via the real dispatch path

        [Test]
        public void SetTransform_StringTarget_ResolvesAndApplies()
        {
            var go = new GameObject("CLI_ObjRefDispatch");
            try
            {
                using (var server = new PipelineTestServer())
                {
                    var resp = server.Execute("set_transform",
                        new { target = "/CLI_ObjRefDispatch", position = new[] { 1f, 2f, 3f } });

                    Assert.IsTrue(resp.IsSuccess,
                        $"set_transform with a string target should succeed (string->ObjectRef coercion): {resp.Error}");
                }

                Assert.AreEqual(new Vector3(1f, 2f, 3f), go.transform.localPosition,
                    "The string handle should have resolved to the GameObject and applied the transform");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        #endregion
    }
}
