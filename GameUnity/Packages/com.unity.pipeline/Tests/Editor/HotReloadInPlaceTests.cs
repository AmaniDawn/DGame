using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Unity.Pipeline.HotReload;
using Unity.Pipeline.Runtime.Commands;
using UnityEngine;

namespace Unity.Pipeline.Tests.Editor
{
    /// <summary>
    /// Top-level probe type woven by the HotReloadInPlace ILPostProcessor. Must be top-level (not
    /// nested) so the generated override can reference it by name from a separate assembly.
    /// </summary>
    public class InPlaceReloadProbe
    {
        public int baseline;
        public int hotValue;

        [HotReload]
        public void Compute()
        {
            baseline = 1; // original body
        }
    }

    /// <summary>
    /// Tests for the in-place hot reload workflow, covering all three layers:
    ///  - compile-time IL weaving + runtime dispatch (the ILPostProcessor),
    ///  - the Roslyn semantic-model transform + accessibility validation,
    ///  - the full reload_file pipeline end to end.
    /// </summary>
    public class HotReloadInPlaceTests
    {
        // -------- weaving fixtures --------

        public class WeaveTarget
        {
            public int ticks;
            public bool overrideRan;

            [HotReload]
            public void Tick()
            {
                ticks++; // original body
            }
        }

        public static class WeaveOverrides
        {
            [HotReloadOverrideMethod("WeaveTarget.Tick")]
            public static void Tick(WeaveTarget instance)
            {
                instance.overrideRan = true;
            }
        }

        private const string PublicSource = @"
using UnityEngine;
using Unity.Pipeline.HotReload;

public class Demo : MonoBehaviour
{
    public float speed = 1f;

    [HotReload]
    void Tick()
    {
        var dt = Time.deltaTime;
        transform.position += Vector3.right * speed * dt;
    }
}";

        private static Dictionary<string, string> Bodies(params string[] names)
        {
            var d = new Dictionary<string, string>();
            foreach (var n in names) d[n] = "";
            return d;
        }

        [SetUp]
        public void Setup() => HotReloadRegistry.ClearAllForTesting();

        [TearDown]
        public void TearDown() => HotReloadRegistry.ClearAllForTesting();

        // -------- weaving + dispatch --------

        [Test]
        public void NoOverride_RunsOriginalBody()
        {
            var t = new WeaveTarget();
            t.Tick();

            Assert.AreEqual(1, t.ticks, "Original body should run when no override is registered");
            Assert.IsFalse(t.overrideRan);
        }

        [Test]
        public void WithOverride_WovenDispatchInvokesOverride()
        {
            HotReloadRegistry.RegisterReloadableMethod(
                typeof(WeaveTarget).GetMethod(nameof(WeaveTarget.Tick)),
                new HotReloadWithOverridesAttribute());

            var registered = HotReloadRegistry.RegisterMethodOverride(
                typeof(WeaveOverrides).GetMethod(nameof(WeaveOverrides.Tick)),
                new HotReloadOverrideMethodAttribute("WeaveTarget.Tick"),
                typeof(WeaveOverrides),
                out var reason);

            Assert.IsTrue(registered, reason);

            var t = new WeaveTarget();
            t.Tick();

            Assert.IsTrue(t.overrideRan, "Woven dispatch should have routed Tick() to the override");
            Assert.AreEqual(0, t.ticks, "Original body should be skipped when the override runs");
        }

        // -------- semantic transform + accessibility --------

        [Test]
        public void Transform_QualifiesInstanceMembers_LeavesLocalsAndStatics()
        {
            var output = SourceCodeTransformer.TransformMethodBodies(
                Bodies("Tick"), "Demo", new Dictionary<string, MethodSignatureInfo>(), PublicSource);

            StringAssert.Contains("[HotReloadOverrideMethod(\"Demo.Tick\")]", output);
            StringAssert.Contains("public static void Tick(Demo instance", output);

            // Instance members are qualified.
            StringAssert.Contains("instance.transform", output);
            StringAssert.Contains("instance.speed", output);

            // Statics and locals are left alone.
            StringAssert.Contains("Time.deltaTime", output);
            StringAssert.Contains("Vector3.right", output);
            StringAssert.DoesNotContain("instance.dt", output);
            StringAssert.DoesNotContain("instance.Time", output);
            StringAssert.DoesNotContain("instance.Vector3", output);
        }

        [Test]
        public void Transform_WithLineDirectives_MapsBodyToOriginalFile()
        {
            // The body's opening brace sits on line 11 of PublicSource.
            var path = @"C:\proj\Demo.cs";
            var output = SourceCodeTransformer.TransformMethodBodies(
                Bodies("Tick"), "Demo", new Dictionary<string, MethodSignatureInfo>(), PublicSource,
                emitLineDirectives: true, originalFilePath: path);

            // #line maps the body back to the original file (backslashes escaped for the literal),
            // bracketed by #line hidden so the generated scaffolding isn't attributed to user code.
            StringAssert.Contains("#line 11 \"C:\\\\proj\\\\Demo.cs\"", output);
            StringAssert.Contains("#line hidden", output);

            // The instance qualification still happens in this mode.
            StringAssert.Contains("instance.transform", output);
            StringAssert.Contains("instance.speed", output);
        }

        [Test]
        public void Transform_WithoutLineDirectives_EmitsNoLineDirectives()
        {
            var output = SourceCodeTransformer.TransformMethodBodies(
                Bodies("Tick"), "Demo", new Dictionary<string, MethodSignatureInfo>(), PublicSource);

            StringAssert.DoesNotContain("#line", output);
        }

        [Test]
        public void Validator_AllowsPublicMembers()
        {
            var result = AccessibilityValidator.ValidatePublicAccess(PublicSource, Bodies("Tick"), "Demo");
            Assert.IsTrue(result.IsValid, result.GetFormattedErrorMessage());
        }

        [Test]
        public void Validator_FlagsPrivateMember()
        {
            var source = @"
using UnityEngine;
using Unity.Pipeline.HotReload;

public class Demo : MonoBehaviour
{
    private float secretSpeed = 1f;

    [HotReload]
    void Tick()
    {
        transform.position += Vector3.right * secretSpeed * Time.deltaTime;
    }
}";
            var result = AccessibilityValidator.ValidatePublicAccess(source, Bodies("Tick"), "Demo");

            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Violations.Exists(v => v.MemberName == "secretSpeed"),
                "Should flag the private field 'secretSpeed'");
            StringAssert.DoesNotContain("transform", result.GetFormattedErrorMessage(),
                "Public/base members like 'transform' must not be flagged");
        }

        // -------- end to end (full reload_file pipeline) --------

        [Test]
        public void ReloadFileInPlace_EditedBody_AppliesViaWovenDispatch()
        {
            // Auto-discovery would do this at play start; register the target explicitly for the test.
            HotReloadRegistry.RegisterReloadableMethod(
                typeof(InPlaceReloadProbe).GetMethod(nameof(InPlaceReloadProbe.Compute)),
                new HotReloadWithOverridesAttribute());

            // The "edited" source: Compute now writes hotValue instead of baseline.
            var editedSource = @"
using Unity.Pipeline.HotReload;
namespace Unity.Pipeline.Tests.Editor
{
    public class InPlaceReloadProbe
    {
        public int baseline;
        public int hotValue;

        [HotReload]
        public void Compute()
        {
            hotValue = 99;
        }
    }
}";
            var path = Path.Combine(Application.temporaryCachePath, "InPlaceReloadProbe_edit.cs");
            File.WriteAllText(path, editedSource);

            try
            {
                var response = HotReloadCommands.ReloadFile(path);
                Assert.IsTrue(response.Success, $"reload_file should succeed. Error: {response.ErrorDetails}");

                var probe = new InPlaceReloadProbe();
                probe.Compute();

                Assert.AreEqual(99, probe.hotValue, "Edited [HotReload] body should run via the woven dispatch");
                Assert.AreEqual(0, probe.baseline, "Original body should be skipped when the override runs");
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Test]
        public void ReloadFileInPlace_WithPdb_StillAppliesViaWovenDispatch()
        {
            // Functional parity: the --pdb path (#line directives + portable PDB, unoptimized) must
            // still compile and dispatch exactly like the default path.
            HotReloadRegistry.RegisterReloadableMethod(
                typeof(InPlaceReloadProbe).GetMethod(nameof(InPlaceReloadProbe.Compute)),
                new HotReloadWithOverridesAttribute());

            var editedSource = @"
using Unity.Pipeline.HotReload;
namespace Unity.Pipeline.Tests.Editor
{
    public class InPlaceReloadProbe
    {
        public int baseline;
        public int hotValue;

        [HotReload]
        public void Compute()
        {
            hotValue = 99;
        }
    }
}";
            var path = Path.Combine(Application.temporaryCachePath, "InPlaceReloadProbe_pdb_edit.cs");
            File.WriteAllText(path, editedSource);

            try
            {
                var response = HotReloadCommands.ReloadFile(path, pdb: true);
                Assert.IsTrue(response.Success, $"reload_file --pdb should succeed. Error: {response.ErrorDetails}");

                var probe = new InPlaceReloadProbe();
                probe.Compute();

                Assert.AreEqual(99, probe.hotValue, "Edited body should run via woven dispatch even with --pdb");
                Assert.AreEqual(0, probe.baseline, "Original body should be skipped when the override runs");
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }
}
