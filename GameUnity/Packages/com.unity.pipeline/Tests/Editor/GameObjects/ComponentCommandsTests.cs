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
    /// Tests for the component authoring commands (CLI-192), exercised directly and via
    /// <see cref="PipelineTestServer"/>. Covers add/remove, a serialized-property round trip
    /// (set -> get) over primitive + enum + Vector types, object-reference assignment via a resolved
    /// handle, and the Undo-revert contract for a property mutation.
    /// </summary>
    public class ComponentCommandsTests
    {
        private readonly List<GameObject> m_Spawned = new List<GameObject>();

        private GameObject Track(GameObject go)
        {
            m_Spawned.Add(go);
            return go;
        }

        private static ObjectRef RefTo(Object obj) => new ObjectRef { InstanceId = PipelineUtils.GetObjectId(obj) };

        // Temp asset folder for the CLI-220 follow-up tests (asset references + arrays); cleaned in TearDown.
        private const string AssetRoot = "Assets/__CLI220Test";

        [TearDown]
        public void TearDown()
        {
            foreach (var go in m_Spawned)
            {
                if (go != null)
                    Object.DestroyImmediate(go);
            }

            m_Spawned.Clear();

            if (AssetDatabase.IsValidFolder(AssetRoot))
            {
                AssetDatabase.DeleteAsset(AssetRoot);
                AssetDatabase.Refresh();
            }
        }

        private static void EnsureAssetRoot()
        {
            if (!AssetDatabase.IsValidFolder(AssetRoot))
                AssetDatabase.CreateFolder("Assets", "__CLI220Test");
        }

        /// <summary>Pick a shader that resolves on this project's active pipeline.</summary>
        private static Shader KnownShader()
        {
            return Shader.Find("Standard")
                ?? Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Sprites/Default");
        }

        /// <summary>Create a temporary Material asset under <see cref="AssetRoot"/> and return the loaded asset.</summary>
        private static Material CreateTempMaterial(string name)
        {
            EnsureAssetRoot();
            var mat = new Material(KnownShader()) { name = name };
            var path = $"{AssetRoot}/{name}.mat";
            AssetDatabase.CreateAsset(mat, path);
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<Material>(path);
        }

        /// <summary>Create a temporary Mesh asset (a shaderless, version-stable scalar object reference).</summary>
        private static Mesh CreateTempMesh(string name)
        {
            EnsureAssetRoot();
            var mesh = new Mesh { name = name };
            var path = $"{AssetRoot}/{name}.asset";
            AssetDatabase.CreateAsset(mesh, path);
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<Mesh>(path);
        }

        #region Direct

        [Test]
        public void AddComponent_ByShortName_Adds()
        {
            var go = Track(new GameObject("AddComp_CLI192"));
            var result = ComponentCommands.AddComponent(RefTo(go), "Rigidbody");

            Assert.IsNotNull(go.GetComponent<Rigidbody>(), "Rigidbody should be added");
            Assert.AreEqual("Rigidbody", result.Type);
        }

        [Test]
        public void AddComponent_ByFullyQualifiedName_Adds()
        {
            var go = Track(new GameObject("AddCompFq_CLI192"));
            ComponentCommands.AddComponent(RefTo(go), "UnityEngine.Rigidbody");
            Assert.IsNotNull(go.GetComponent<Rigidbody>());
        }

        [Test]
        public void RemoveComponent_ByGameObjectAndType_Removes()
        {
            var go = Track(new GameObject("RemoveComp_CLI192"));
            go.AddComponent<Rigidbody>();

            ComponentCommands.RemoveComponent(RefTo(go), "Rigidbody");

            Assert.IsNull(go.GetComponent<Rigidbody>(), "Rigidbody should be removed");
        }

        [Test]
        public void SetGetProperties_PrimitiveRoundTrip()
        {
            var go = Track(new GameObject("PropPrim_CLI192"));
            var rb = go.AddComponent<Rigidbody>();

            ComponentCommands.SetComponentProperties(RefTo(rb),
                new JObject { ["m_Mass"] = 12.5f });

            Assert.AreEqual(12.5f, rb.mass, 0.001f, "Mass should be set on the live component");

            var read = ComponentCommands.GetComponentProperties(RefTo(rb));
            Assert.AreEqual(12.5, read.Properties["m_Mass"].ToObject<double>(), 0.001,
                "get should read back the value that set wrote");
        }

        [Test]
        public void SetProperties_EnumByName_Applies()
        {
            var go = Track(new GameObject("PropEnum_CLI192"));
            var rb = go.AddComponent<Rigidbody>();

            // m_CollisionDetection maps to the CollisionDetectionMode enum.
            ComponentCommands.SetComponentProperties(RefTo(rb),
                new JObject { ["m_CollisionDetection"] = "Continuous" });

            Assert.AreEqual(CollisionDetectionMode.Continuous, rb.collisionDetectionMode,
                "Enum set by name should apply");
        }

        [Test]
        public void SetProperties_VectorType_Applies()
        {
            var go = Track(new GameObject("PropVec_CLI192"));
            var box = go.AddComponent<BoxCollider>();

            ComponentCommands.SetComponentProperties(RefTo(box),
                new JObject { ["m_Size"] = new JArray(2f, 3f, 4f) });

            Assert.AreEqual(new Vector3(2, 3, 4), box.size, "Vector3 property should be set from a JSON array");
        }

        [Test]
        public void SetProperties_ObjectReference_ByResolvedHandle()
        {
            // Create a real Mesh asset on disk so it resolves through ObjectResolver by path. A Mesh is
            // a version-stable, single object-reference target (MeshFilter.m_Mesh).
            const string dir = "Assets/__CLI192ObjRefTest";
            if (!AssetDatabase.IsValidFolder(dir))
                AssetDatabase.CreateFolder("Assets", "__CLI192ObjRefTest");
            var meshPath = dir + "/RefMesh.asset";
            var mesh = new Mesh { name = "RefMesh_CLI192" };
            AssetDatabase.CreateAsset(mesh, meshPath);
            AssetDatabase.SaveAssets();

            try
            {
                var go = Track(new GameObject("PropObjRef_CLI192"));
                var mf = go.AddComponent<MeshFilter>();

                // MeshFilter.m_Mesh is a single Mesh object reference.
                ComponentCommands.SetComponentProperties(RefTo(mf),
                    new JObject { ["m_Mesh"] = new JObject { ["path"] = meshPath } });

                Assert.IsNotNull(mf.sharedMesh, "Object reference should be assigned from the resolved handle");
                Assert.AreEqual(mesh, mf.sharedMesh, "Should be the exact resolved asset");
            }
            finally
            {
                AssetDatabase.DeleteAsset(dir);
                AssetDatabase.Refresh();
            }
        }

        [Test]
        public void SetProperties_UnknownProperty_Throws()
        {
            var go = Track(new GameObject("PropBad_CLI192"));
            var rb = go.AddComponent<Rigidbody>();

            Assert.Throws<System.ArgumentException>(() =>
                ComponentCommands.SetComponentProperties(RefTo(rb),
                    new JObject { ["m_NoSuchProperty"] = 1 }));
        }

        [Test]
        public void SetProperties_IsUndone()
        {
            var go = Track(new GameObject("PropUndo_CLI192"));
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 1f;

            ComponentCommands.SetComponentProperties(RefTo(rb), new JObject { ["m_Mass"] = 9f });
            Assert.AreEqual(9f, rb.mass, 0.001f);

            Undo.PerformUndo();
            Assert.AreEqual(1f, rb.mass, 0.001f, "Undo should revert the property change");
        }

        #endregion

        #region ViaClient

        [Test]
        public void AddComponent_ViaClient_Succeeds()
        {
            var go = Track(new GameObject("ViaAdd_CLI192"));

            using (var server = new PipelineTestServer())
            {
                var response = server.Execute("add_component",
                    new { target = new { instanceId = PipelineUtils.GetObjectId(go) }, type = "Rigidbody" });

                Assert.IsTrue(response.IsSuccess, $"add_component should succeed: {response.Error}");
                Assert.IsTrue(response.HasValidJson, "Response should have valid JSON");
                Assert.IsNotNull(go.GetComponent<Rigidbody>(), "Rigidbody should be added via client");
            }
        }

        [Test]
        public void SetComponentProperties_ViaClient_RoundTrips()
        {
            var go = Track(new GameObject("ViaProp_CLI192"));
            var rb = go.AddComponent<Rigidbody>();

            using (var server = new PipelineTestServer())
            {
                var response = server.Execute("set_component_properties", new
                {
                    target = new { instanceId = PipelineUtils.GetObjectId(rb) },
                    properties = new { m_Mass = 7.0 }
                });

                Assert.IsTrue(response.IsSuccess, $"set_component_properties should succeed: {response.Error}");
                Assert.AreEqual(7f, rb.mass, 0.001f, "Mass should be set via client");

                var massBack = response.JsonResponse["result"]?["properties"]?["m_Mass"]?.ToObject<double>();
                Assert.AreEqual(7.0, massBack.Value, 0.001, "Result should echo the committed value");
            }
        }

        #endregion

        #region CLI-220 follow-ups — asset references + array properties

        [Test]
        public void SetProperties_AssetReference_ByInstanceId_Assigns()
        {
            // The core of the bug: an ASSET reference addressed by instanceId must actually resolve and
            // assign (it used to silently no-op and report success).
            var mesh = CreateTempMesh("ByInstanceId");
            var go = Track(new GameObject("PropMeshInst_CLI220"));
            var mf = go.AddComponent<MeshFilter>();

            ComponentCommands.SetComponentProperties(RefTo(mf),
                new JObject { ["m_Mesh"] = new JObject { ["instanceId"] = PipelineUtils.GetObjectId(mesh).RawValue } });

            Assert.AreEqual(mesh, mf.sharedMesh, "Asset reference by instanceId should be assigned");
        }

        [Test]
        public void SetProperties_AssetReference_ByGuid_Assigns()
        {
            var mesh = CreateTempMesh("ByGuid");
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh, out var guid, out long _);

            var go = Track(new GameObject("PropMeshGuid_CLI220"));
            var mf = go.AddComponent<MeshFilter>();

            ComponentCommands.SetComponentProperties(RefTo(mf),
                new JObject { ["m_Mesh"] = new JObject { ["guid"] = guid } });

            Assert.AreEqual(mesh, mf.sharedMesh, "Asset reference by guid should be assigned");
        }

        [Test]
        public void SetProperties_UnresolvableReference_Throws()
        {
            // Previously this returned success while assigning nothing. It must now fail loudly.
            var go = Track(new GameObject("PropBadRef_CLI220"));
            var mf = go.AddComponent<MeshFilter>();

            Assert.Throws<System.ArgumentException>(() =>
                ComponentCommands.SetComponentProperties(RefTo(mf),
                    new JObject { ["m_Mesh"] = new JObject { ["instanceId"] = 999999999 } }),
                "An unresolvable object reference must throw, not silently succeed.");
        }

        [Test]
        public void SetProperties_ArrayOfObjectReferences_Assigns()
        {
            var matA = CreateTempMaterial("ArrA");
            var matB = CreateTempMaterial("ArrB");

            var go = Track(new GameObject("PropMatArray_CLI220"));
            var renderer = go.AddComponent<MeshRenderer>();

            // MeshRenderer.m_Materials is an object-reference array (Generic+isArray).
            ComponentCommands.SetComponentProperties(RefTo(renderer), new JObject
            {
                ["m_Materials"] = new JArray(
                    new JObject { ["path"] = AssetDatabase.GetAssetPath(matA) },
                    new JObject { ["path"] = AssetDatabase.GetAssetPath(matB) })
            });

            Assert.AreEqual(2, renderer.sharedMaterials.Length, "Array property should be sized to the JSON array");
            Assert.AreEqual(matA, renderer.sharedMaterials[0]);
            Assert.AreEqual(matB, renderer.sharedMaterials[1]);
        }

        [Test]
        public void GetProperties_Array_ReadsBack()
        {
            var matA = CreateTempMaterial("ReadA");
            var matB = CreateTempMaterial("ReadB");

            var go = Track(new GameObject("PropMatArrayRead_CLI220"));
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = new[] { matA, matB };

            var read = ComponentCommands.GetComponentProperties(RefTo(renderer));
            var materials = read.Properties["m_Materials"] as JArray;

            Assert.IsNotNull(materials, "Array property should read back as a JSON array");
            Assert.AreEqual(2, materials.Count, "Array read should report both elements");
        }

        [Test]
        public void SetProperties_ViaClient_AssetByCapitalInstanceID_Assigns()
        {
            // The agent's exact repro: a handle with a capital-D "instanceID" key. It must bind
            // (case-insensitive) and assign the asset over the wire.
            var mesh = CreateTempMesh("ViaCapital");
            var go = Track(new GameObject("PropMeshCap_CLI220"));
            var mf = go.AddComponent<MeshFilter>();

            using (var server = new PipelineTestServer())
            {
                var response = server.Execute("set_component_properties", new
                {
                    target = new { instanceId = PipelineUtils.GetObjectId(mf) },
                    properties = new { m_Mesh = new { instanceID = PipelineUtils.GetObjectId(mesh) } }
                });

                Assert.IsTrue(response.IsSuccess, $"set_component_properties should succeed: {response.Error}");
                Assert.AreEqual(mesh, mf.sharedMesh, "Capital 'instanceID' handle must resolve and assign the asset");
            }
        }

        [Test]
        public void SetProperties_ViaClient_MaterialArray_Assigns()
        {
            var matA = CreateTempMaterial("ViaArrA");
            var matB = CreateTempMaterial("ViaArrB");
            var go = Track(new GameObject("PropMatArrayVia_CLI220"));
            var renderer = go.AddComponent<MeshRenderer>();

            using (var server = new PipelineTestServer())
            {
                var response = server.Execute("set_component_properties", new
                {
                    target = new { instanceId = PipelineUtils.GetObjectId(renderer) },
                    properties = new
                    {
                        m_Materials = new object[]
                        {
                            new { path = AssetDatabase.GetAssetPath(matA) },
                            new { path = AssetDatabase.GetAssetPath(matB) }
                        }
                    }
                });

                Assert.IsTrue(response.IsSuccess, $"set_component_properties (array) should succeed: {response.Error}");
                Assert.AreEqual(2, renderer.sharedMaterials.Length, "Material array should be set via client");
                Assert.AreEqual(matA, renderer.sharedMaterials[0]);
                Assert.AreEqual(matB, renderer.sharedMaterials[1]);
            }
        }

        #endregion
    }
}
