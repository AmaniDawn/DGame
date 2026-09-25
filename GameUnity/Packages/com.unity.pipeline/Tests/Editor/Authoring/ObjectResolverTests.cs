using NUnit.Framework;
using Unity.Pipeline.Editor.Authoring;
using Unity.Pipeline.Models;
using UnityEngine;
using Object = UnityEngine.Object;
using Unity.Pipeline;

namespace Unity.Pipeline.Tests.Editor.Authoring
{
    /// <summary>
    /// Tests for the object-reference resolver foundation (CLI-190): each handle form resolves back
    /// to the same object, and Describe produces a canonical identity.
    /// </summary>
    public class ObjectResolverTests
    {
        private GameObject m_SceneObject;

        [TearDown]
        public void TearDown()
        {
            if (m_SceneObject != null)
                Object.DestroyImmediate(m_SceneObject);
            m_SceneObject = null;
        }

        [Test]
        public void Describe_SceneObject_ProducesInstanceIdAndHierarchyPath()
        {
            m_SceneObject = new GameObject("CLI190_Root");
            var child = new GameObject("Child");
            child.transform.SetParent(m_SceneObject.transform);

            var info = ObjectResolver.Describe(child);

            Assert.IsNotNull(info);
            Assert.AreEqual(PipelineUtils.GetObjectId(child), info.InstanceId);
            Assert.AreEqual("/CLI190_Root/Child", info.HierarchyPath);
            Assert.AreEqual("GameObject", info.Type);
            Assert.IsNull(info.AssetPath, "A scene object should not report an asset path");
        }

        [Test]
        public void Resolve_ByInstanceId_ReturnsSameObject()
        {
            m_SceneObject = new GameObject("CLI190_ById");
            var handle = new ObjectRef { InstanceId = PipelineUtils.GetObjectId(m_SceneObject) };

            Assert.IsTrue(ObjectResolver.TryResolve(handle, out var obj, out var error), error);
            Assert.AreSame(m_SceneObject, obj);
        }

        [Test]
        public void Resolve_ByHierarchyPath_ReturnsSameObject()
        {
            m_SceneObject = new GameObject("CLI190_ByPath");
            var handle = new ObjectRef { HierarchyPath = "/CLI190_ByPath" };

            Assert.IsTrue(ObjectResolver.TryResolve(handle, out var obj, out var error), error);
            Assert.AreSame(m_SceneObject, obj);
        }

        [Test]
        public void Resolve_EmptyHandle_FailsWithError()
        {
            Assert.IsFalse(ObjectResolver.TryResolve(new ObjectRef(), out _, out var error));
            Assert.IsNotEmpty(error);
        }

        [Test]
        public void Resolve_UnknownGuid_FailsWithError()
        {
            var handle = new ObjectRef { Guid = "00000000000000000000000000000000" };
            Assert.IsFalse(ObjectResolver.TryResolve(handle, out _, out var error));
            Assert.IsNotEmpty(error);
        }
    }
}
