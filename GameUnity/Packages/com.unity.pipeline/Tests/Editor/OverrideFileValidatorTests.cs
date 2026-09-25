using NUnit.Framework;
using Unity.Pipeline.HotReload;

namespace Unity.Pipeline.Tests.Editor
{
    /// <summary>
    /// Unit tests for OverrideFileValidator - the up-front, parse-only validation used by the
    /// helper-workflow reload_file_override command.
    /// </summary>
    public class OverrideFileValidatorTests
    {
        [Test]
        public void Validate_ProperOverrideFile_IsValid()
        {
            var source = @"
using Unity.Pipeline.HotReload;
using UnityEngine;

public static class BossOverrides
{
    [HotReloadOverrideMethod(""BossController.Update"")]
    public static void TweakedUpdate(BossController instance)
    {
        instance.transform.Rotate(0, 1, 0);
    }
}";
            var result = OverrideFileValidator.Validate(source, "BossOverrides.cs");

            Assert.IsTrue(result.IsValid, result.GetFormattedErrorMessage());
        }

        [Test]
        public void Validate_NoHotReloadMethods_IsInvalid()
        {
            var source = @"
using UnityEngine;

public class BossController : MonoBehaviour
{
    void Update() { }
}";
            var result = OverrideFileValidator.Validate(source, "BossController.cs");

            Assert.IsFalse(result.IsValid);
            Assert.That(result.GetFormattedErrorMessage(), Does.Contain("No [HotReloadOverrideMethod] overrides found"));
        }

        [Test]
        public void Validate_RedeclaresTargetType_IsInvalid()
        {
            // The classic mistake: override lives in the same file that declares the target type.
            var source = @"
using Unity.Pipeline.HotReload;
using UnityEngine;

public class BossController : MonoBehaviour
{
    [HotReloadOverrideMethod(""BossController.Update"")]
    public static void TweakedUpdate(BossController instance) { }
}";
            var result = OverrideFileValidator.Validate(source, "BossController.cs");

            Assert.IsFalse(result.IsValid);
            Assert.That(result.GetFormattedErrorMessage(), Does.Contain("BossController"));
            Assert.That(result.GetFormattedErrorMessage(), Does.Contain("separate file"));
        }

        [Test]
        public void Validate_NonStaticOverride_IsInvalid()
        {
            var source = @"
using Unity.Pipeline.HotReload;

public class Overrides
{
    [HotReloadOverrideMethod(""Player.Update"")]
    public void Update(Player instance) { }
}";
            var result = OverrideFileValidator.Validate(source, "Overrides.cs");

            Assert.IsFalse(result.IsValid);
            Assert.That(result.GetFormattedErrorMessage(), Does.Contain("public static"));
        }

        [Test]
        public void Validate_OverrideWithoutInstanceParameter_IsInvalid()
        {
            var source = @"
using Unity.Pipeline.HotReload;

public static class Overrides
{
    [HotReloadOverrideMethod(""Player.Update"")]
    public static void Update() { }
}";
            var result = OverrideFileValidator.Validate(source, "Overrides.cs");

            Assert.IsFalse(result.IsValid);
            Assert.That(result.GetFormattedErrorMessage(), Does.Contain("first parameter"));
        }
    }
}
