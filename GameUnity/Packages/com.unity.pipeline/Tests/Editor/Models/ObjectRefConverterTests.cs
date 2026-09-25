using NUnit.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Pipeline;
using Unity.Pipeline.Models;

namespace Unity.Pipeline.Tests.Editor.Models
{
    /// <summary>
    /// Tests that <see cref="ObjectRefConverter"/> binds handle-object keys case-INSENSITIVELY
    /// (CLI-220 follow-up). Agents frequently send "instanceID"/"fileID" (capital D); a case-sensitive
    /// lookup dropped those into an empty handle, which then resolved to nothing while the calling
    /// command still reported success.
    /// </summary>
    public class ObjectRefConverterTests
    {
        private static ObjectRef Parse(string json) => JsonConvert.DeserializeObject<ObjectRef>(json);

        [Test]
        public void CapitalInstanceID_BindsToInstanceId()
        {
            var r = Parse("{\"instanceID\": 53816}");
            Assert.IsNotNull(r);
            Assert.AreEqual(ObjectId.FromRaw(53816), r.InstanceId, "capital 'instanceID' should bind to InstanceId");
            Assert.IsFalse(r.IsEmpty, "the handle must not be empty");
        }

        [Test]
        public void LowercaseInstanceId_StillBinds()
        {
            Assert.AreEqual(ObjectId.FromRaw(1234), Parse("{\"instanceId\": 1234}").InstanceId);
        }

        [Test]
        public void CapitalFileID_AndGuid_Bind()
        {
            var r = Parse("{\"guid\":\"abc123\",\"fileID\": 99}");
            Assert.AreEqual("abc123", r.Guid);
            Assert.AreEqual(99L, r.FileId, "capital 'fileID' should bind to FileId");
        }

        [Test]
        public void BareInteger_IsInstanceId()
        {
            var r = JToken.Parse("48184").ToObject<ObjectRef>();
            Assert.AreEqual(ObjectId.FromRaw(48184), r.InstanceId);
        }

        [Test]
        public void StringHandles_Path_And_Hierarchy()
        {
            Assert.AreEqual("Assets/Foo.mat", Parse("\"Assets/Foo.mat\"").Path, "Assets/ string is a path");
            Assert.AreEqual("/Player", Parse("\"/Player\"").HierarchyPath, "leading-slash string is a hierarchy path");
        }
    }
}
