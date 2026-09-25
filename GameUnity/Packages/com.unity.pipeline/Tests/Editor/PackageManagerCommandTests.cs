using System.Linq;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor;
using Unity.Pipeline.Editor.Commands.PackageManager;

namespace Unity.Pipeline.Tests.Editor
{
    /// <summary>
    /// Tests for the Package Manager commands (CLI-203). Coverage is the parts that don't require a
    /// live UPM round-trip: identifier classification, manifest parsing, command discovery + gate
    /// schema, and the confirm/dry-run gate on the mutating add/remove paths (which is enforced before
    /// any Client call, so it never touches the real project). A confirmed add/remove would mutate the
    /// manifest and force a domain reload, so it is intentionally not exercised here.
    /// </summary>
    public class PackageManagerCommandTests
    {
        [SetUp]
        public void SetUp()
        {
            CommandRegistry.SetDiscovery(new TypeCacheCommandDiscovery());
        }

        // ---- Identifier classification ---------------------------------------------------------

        [TestCase("com.unity.foo", PackageSourceKind.Registry)]
        [TestCase("com.unity.foo@1.2.3", PackageSourceKind.Registry)]
        [TestCase("file:../MyLocalPackage", PackageSourceKind.Local)]
        [TestCase("https://github.com/user/repo.git", PackageSourceKind.Git)]
        [TestCase("https://github.com/user/repo.git#v1.0.0", PackageSourceKind.Git)]
        [TestCase("git+https://github.com/user/repo.git", PackageSourceKind.Git)]
        [TestCase("ssh://git@github.com/user/repo.git", PackageSourceKind.Git)]
        [TestCase("git@github.com:user/repo.git", PackageSourceKind.Git)]
        [TestCase("", PackageSourceKind.Unknown)]
        public void Classify_DetectsSource(string identifier, PackageSourceKind expected)
        {
            Assert.AreEqual(expected, PackageIdentifier.Classify(identifier));
        }

        [Test]
        public void TryParse_RegistryWithVersion_SplitsNameAndVersion()
        {
            Assert.IsTrue(PackageIdentifier.TryParse("com.unity.foo@1.2.3", out var parsed, out var error), error);
            Assert.AreEqual(PackageSourceKind.Registry, parsed.Kind);
            Assert.AreEqual("com.unity.foo", parsed.Name);
            Assert.AreEqual("1.2.3", parsed.Version);
            Assert.AreEqual("com.unity.foo@1.2.3", parsed.Identifier);
        }

        [Test]
        public void TryParse_RegistryWithoutVersion_HasNullVersion()
        {
            Assert.IsTrue(PackageIdentifier.TryParse("com.unity.foo", out var parsed, out _));
            Assert.AreEqual("com.unity.foo", parsed.Name);
            Assert.IsNull(parsed.Version);
        }

        [Test]
        public void TryParse_GitWithRevision_ExtractsRevision()
        {
            Assert.IsTrue(PackageIdentifier.TryParse("https://github.com/user/repo.git#v1.0.0", out var parsed, out _));
            Assert.AreEqual(PackageSourceKind.Git, parsed.Kind);
            Assert.AreEqual("v1.0.0", parsed.Version);
        }

        [Test]
        public void TryParse_Empty_Fails()
        {
            Assert.IsFalse(PackageIdentifier.TryParse("   ", out _, out var error));
            StringAssert.Contains("empty", error);
        }

        // ---- Manifest parsing ------------------------------------------------------------------

        [Test]
        public void ReadDependencies_ParsesNameVersionMap()
        {
            const string json = @"{
                ""dependencies"": {
                    ""com.unity.foo"": ""1.2.3"",
                    ""com.unity.bar"": ""file:../bar""
                }
            }";

            var deps = PackageManifest.ReadDependencies(json);

            Assert.AreEqual(2, deps.Count);
            Assert.AreEqual("1.2.3", deps["com.unity.foo"]);
            Assert.AreEqual("file:../bar", deps["com.unity.bar"]);
        }

        [Test]
        public void ReadDependencies_EmptyOrMissing_ReturnsEmptyMap()
        {
            Assert.AreEqual(0, PackageManifest.ReadDependencies("").Count);
            Assert.AreEqual(0, PackageManifest.ReadDependencies("{}").Count);
        }

        // ---- Discovery + schema ----------------------------------------------------------------

        [Test]
        public void AllPackageCommands_AreDiscovered()
        {
            var names = CommandRegistry.DiscoverCommands().Select(c => c.Name).ToList();

            foreach (var command in new[]
                     {
                         "package_list", "package_search", "package_add",
                         "package_remove", "package_resolve", "package_status"
                     })
            {
                Assert.Contains(command, names, $"missing command {command}");
            }
        }

        [Test]
        public void PackageList_InstalledScope_ReturnsResultsSynchronously()
        {
            // Default (installed) scope reads the resolved set directly — no async trigger / poll.
            var response = (PackageListResponse)PackageManagerCommand.PackageList();

            Assert.IsTrue(response.Success);
            Assert.AreEqual("installed", response.Scope);
            Assert.Greater(response.Count, 0, "a Unity project always has at least its built-in packages");
            Assert.AreEqual(response.Count, response.Packages.Count);
            Assert.IsNotNull(response.Manifest, "the manifest state should be included");
            Assert.IsTrue(response.Packages.TrueForAll(p => p.IsInstalled), "installed scope: every entry is installed");
        }

        [Test]
        public void PackageList_DirectDependenciesOnly_AreFewerThanAll()
        {
            var all = (PackageListResponse)PackageManagerCommand.PackageList("installed", includeIndirect: true);
            var direct = (PackageListResponse)PackageManagerCommand.PackageList("installed", includeIndirect: false);

            Assert.LessOrEqual(direct.Count, all.Count);
            Assert.IsTrue(direct.Packages.TrueForAll(p => p.IsDirectDependency), "include_indirect=false should drop transitive deps");
        }

        [Test]
        public void PackageList_UnknownScope_Fails()
        {
            var response = (PackageListResponse)PackageManagerCommand.PackageList("bogus");

            Assert.IsFalse(response.Success);
            StringAssert.Contains("scope", response.Message);
        }

        [Test]
        public void PackageStatus_IsOffMainThread()
        {
            var command = CommandRegistry.DiscoverCommands().First(c => c.Name == "package_status");
            Assert.IsFalse(command.MainThreadRequired, "package_status only reads a file; it must respond while an op is in flight");
        }

        [Test]
        public void PackageList_ExposesScopeArg()
        {
            var command = CommandRegistry.DiscoverCommands().First(c => c.Name == "package_list");
            var schema = JObject.Parse(JsonSchemaGenerator.GenerateCommandSchema(command));
            var properties = (JObject)schema["properties"];

            Assert.AreEqual("string", properties["scope"]?["type"]?.ToString());
            Assert.AreEqual("boolean", properties["include_indirect"]?["type"]?.ToString());
        }

        [Test]
        public void PackageSearch_ExposesQueryArg()
        {
            var command = CommandRegistry.DiscoverCommands().First(c => c.Name == "package_search");
            var schema = JObject.Parse(JsonSchemaGenerator.GenerateCommandSchema(command));
            var properties = (JObject)schema["properties"];

            Assert.AreEqual("string", properties["query"]?["type"]?.ToString());
            Assert.AreEqual("boolean", properties["offline"]?["type"]?.ToString());
        }

        [Test]
        public void PackageAdd_ExposesGateArgs()
        {
            var command = CommandRegistry.DiscoverCommands().First(c => c.Name == "package_add");
            var schema = JObject.Parse(JsonSchemaGenerator.GenerateCommandSchema(command));
            var properties = (JObject)schema["properties"];

            Assert.AreEqual("string", properties["identifier"]?["type"]?.ToString());
            Assert.AreEqual("boolean", properties["confirm"]?["type"]?.ToString());
            Assert.AreEqual("boolean", properties["dry_run"]?["type"]?.ToString());
            Assert.AreEqual("boolean", properties["wait"]?["type"]?.ToString(), "add should expose the dual-mode wait flag");

            var required = schema["required"]?.ToObject<string[]>();
            CollectionAssert.Contains(required, "identifier");
        }

        // ---- Gate behaviour (no live UPM op) ---------------------------------------------------

        [Test]
        public void PackageAdd_InvalidIdentifier_Fails()
        {
            var response = (PackageMutationResponse)PackageManagerCommand.PackageAdd("", confirm: true);

            Assert.IsFalse(response.Success);
            Assert.AreEqual("failed", response.Status);
        }

        [Test]
        public void PackageAdd_WithoutConfirm_IsRejected()
        {
            // confirm=false → the command refuses before Client.Add is ever called.
            var response = (PackageMutationResponse)PackageManagerCommand.PackageAdd("com.unity.foo@1.2.3", confirm: false);

            Assert.IsFalse(response.Success);
            Assert.AreEqual("rejected", response.Status);
            Assert.IsFalse(response.Applied);
        }

        [Test]
        public void PackageAdd_DryRun_PreviewsWithoutApplying()
        {
            var response = (PackageMutationResponse)PackageManagerCommand.PackageAdd("com.unity.foo@1.2.3", confirm: false, dryRun: true);

            Assert.IsTrue(response.Success);
            Assert.IsTrue(response.DryRun);
            Assert.AreEqual("dry_run", response.Status);
            Assert.IsFalse(response.Applied);
            StringAssert.Contains("com.unity.foo", response.Plan);
        }

        [Test]
        public void PackageRemove_EmptyName_Fails()
        {
            var response = (PackageMutationResponse)PackageManagerCommand.PackageRemove("", confirm: true);

            Assert.IsFalse(response.Success);
            Assert.AreEqual("failed", response.Status);
        }

        [Test]
        public void PackageRemove_WithoutConfirm_IsRejected()
        {
            var response = (PackageMutationResponse)PackageManagerCommand.PackageRemove("com.unity.foo", confirm: false);

            Assert.IsFalse(response.Success);
            Assert.AreEqual("rejected", response.Status);
            Assert.IsFalse(response.Applied);
        }
    }
}
