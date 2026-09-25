using System.Linq;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using Unity.Pipeline.Editor.Authoring;
using Unity.Pipeline.Editor.Commands.Animation;
using Unity.Pipeline.Models;
using UnityEditor;
using UnityEngine;

namespace Unity.Pipeline.Tests.Editor.Animation
{
    /// <summary>
    /// Tests for CLI-214 Group A (animation clips): create clip with frame rate + loop, set/get/remove
    /// float curves, the overwrite-not-duplicate behaviour, the destructive remove guard, dry_run, and
    /// the sandbox guard. Assets are generated in-test under a throwaway root and cleaned up.
    /// </summary>
    public class AnimationClipCommandsTests
    {
        private const string Root = "Assets/__CLI214AnimTest";

        [TearDown]
        public void TearDown()
        {
            ProjectPaths.ResetAuthoringRoot();
            if (AssetDatabase.IsValidFolder(Root))
            {
                AssetDatabase.DeleteAsset(Root);
                AssetDatabase.Refresh();
            }
        }

        private static ObjectRef PathRef(string path) => new ObjectRef { Path = path };

        private static JArray TwoKeys() => new JArray
        {
            new JObject { ["time"] = 0f, ["value"] = 0f },
            new JObject { ["time"] = 1f, ["value"] = 5f }
        };

        [Test]
        public void CreateAnimationClip_SetsFrameRateAndLoop()
        {
            var path = Root + "/Clips/Walk.anim";
            var result = AnimationClipCommands.CreateAnimationClip(path, frameRate: 30f, loop: true);

            Assert.AreEqual(path, result.AssetPath);

            var loaded = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            Assert.IsNotNull(loaded, "Clip should exist on disk");
            Assert.AreEqual(30f, loaded.frameRate, 0.001f);

            var info = AnimationClipCommands.GetAnimationClip(PathRef(path));
            Assert.AreEqual(30f, info.FrameRate, 0.001f);
            Assert.IsTrue(info.Loop, "loop should be reflected by get_animation_clip");
        }

        [Test]
        public void CreateAnimationClip_OverwriteWithoutConfirm_Throws()
        {
            var path = Root + "/Clips/Dup.anim";
            AnimationClipCommands.CreateAnimationClip(path);

            Assert.Throws<System.ArgumentException>(() => AnimationClipCommands.CreateAnimationClip(path));
            Assert.DoesNotThrow(() => AnimationClipCommands.CreateAnimationClip(path, confirm: true));
        }

        [Test]
        public void CreateAnimationClip_OutOfProject_Throws()
        {
            Assert.Throws<System.ArgumentException>(() => AnimationClipCommands.CreateAnimationClip("../Outside.anim"));
            Assert.Throws<System.ArgumentException>(() => AnimationClipCommands.CreateAnimationClip("Assets/../Outside.anim"));
        }

        [Test]
        public void CreateAnimationClip_DryRun_WritesNothing()
        {
            var path = Root + "/Clips/DryRun.anim";
            var result = AnimationClipCommands.CreateAnimationClip(path, dryRun: true);

            Assert.AreEqual(path, result.AssetPath);
            Assert.IsNull(AssetDatabase.LoadMainAssetAtPath(path), "dry_run must not write a clip");
        }

        [Test]
        public void SetAnimationCurve_AddsCurveWithKeys()
        {
            var path = Root + "/Clips/Curve.anim";
            AnimationClipCommands.CreateAnimationClip(path);

            var result = AnimationClipCommands.SetAnimationCurve(
                PathRef(path), path: "", type: "Transform", property: "m_LocalPosition.x", keys: TwoKeys());

            Assert.AreEqual(2, result.KeyCount);
            Assert.AreEqual("m_LocalPosition.x", result.Binding.Property);

            var info = AnimationClipCommands.GetAnimationClip(PathRef(path), includeKeys: true);
            Assert.AreEqual(1, info.Bindings.Count);
            var binding = info.Bindings[0];
            Assert.AreEqual("Transform", binding.Type);
            Assert.AreEqual("m_LocalPosition.x", binding.Property);
            Assert.AreEqual(2, binding.KeyCount);
            Assert.IsNotNull(binding.Keys);
            Assert.AreEqual(2, binding.Keys.Count);
            Assert.AreEqual(0f, binding.Keys[0].Value, 0.001f);
            Assert.AreEqual(5f, binding.Keys[1].Value, 0.001f);
        }

        [Test]
        public void SetAnimationCurve_ReplaceExisting_Overwrites()
        {
            var path = Root + "/Clips/Replace.anim";
            AnimationClipCommands.CreateAnimationClip(path);

            AnimationClipCommands.SetAnimationCurve(PathRef(path), "", "Transform", "m_LocalPosition.x", TwoKeys());

            var threeKeys = new JArray
            {
                new JObject { ["time"] = 0f, ["value"] = 0f },
                new JObject { ["time"] = 0.5f, ["value"] = 2f },
                new JObject { ["time"] = 1f, ["value"] = 4f }
            };
            var result = AnimationClipCommands.SetAnimationCurve(PathRef(path), "", "Transform", "m_LocalPosition.x", threeKeys);
            Assert.AreEqual(3, result.KeyCount);

            var info = AnimationClipCommands.GetAnimationClip(PathRef(path));
            // Same binding => not duplicated; the single binding now has 3 keys.
            Assert.AreEqual(1, info.Bindings.Count, "Replacing a binding must not duplicate it");
            Assert.AreEqual(3, info.Bindings[0].KeyCount);
        }

        [Test]
        public void SetAnimationCurve_DryRun_WritesNothing()
        {
            var path = Root + "/Clips/DryCurve.anim";
            AnimationClipCommands.CreateAnimationClip(path);

            var result = AnimationClipCommands.SetAnimationCurve(
                PathRef(path), "", "Transform", "m_LocalPosition.x", TwoKeys(), dryRun: true);
            Assert.AreEqual(2, result.KeyCount);

            var info = AnimationClipCommands.GetAnimationClip(PathRef(path));
            Assert.AreEqual(0, info.Bindings.Count, "dry_run must not write a curve");
        }

        [Test]
        public void RemoveAnimationCurve_WithoutConfirm_Throws()
        {
            var path = Root + "/Clips/Remove.anim";
            AnimationClipCommands.CreateAnimationClip(path);
            AnimationClipCommands.SetAnimationCurve(PathRef(path), "", "Transform", "m_LocalPosition.x", TwoKeys());

            Assert.Throws<System.ArgumentException>(
                () => AnimationClipCommands.RemoveAnimationCurve(PathRef(path), "", "Transform", "m_LocalPosition.x"),
                "remove without confirm should be rejected");

            var info = AnimationClipCommands.GetAnimationClip(PathRef(path));
            Assert.AreEqual(1, info.Bindings.Count, "binding should survive a refused remove");
        }

        [Test]
        public void RemoveAnimationCurve_WithConfirm_Removes()
        {
            var path = Root + "/Clips/Remove2.anim";
            AnimationClipCommands.CreateAnimationClip(path);
            AnimationClipCommands.SetAnimationCurve(PathRef(path), "", "Transform", "m_LocalPosition.x", TwoKeys());

            AnimationClipCommands.RemoveAnimationCurve(PathRef(path), "", "Transform", "m_LocalPosition.x", confirm: true);

            var info = AnimationClipCommands.GetAnimationClip(PathRef(path));
            Assert.AreEqual(0, info.Bindings.Count, "binding should be gone after a confirmed remove");
        }

        [Test]
        public void SetAnimationCurve_UnknownType_Throws()
        {
            var path = Root + "/Clips/BadType.anim";
            AnimationClipCommands.CreateAnimationClip(path);

            Assert.Throws<System.ArgumentException>(
                () => AnimationClipCommands.SetAnimationCurve(PathRef(path), "", "NotARealComponent__CLI214", "m_X", TwoKeys()));
        }

        [Test]
        public void SetAnimationCurve_ViaClient_Succeeds()
        {
            var path = Root + "/ViaClient/Curve.anim";
            AnimationClipCommands.CreateAnimationClip(path);

            using (var server = new PipelineTestServer())
            {
                var response = server.Execute("set_animation_curve", new
                {
                    clip = new { path },
                    type = "Transform",
                    property = "m_LocalPosition.x",
                    keys = new object[]
                    {
                        new { time = 0f, value = 0f },
                        new { time = 1f, value = 5f }
                    }
                });

                Assert.IsTrue(response.IsSuccess, $"set_animation_curve should succeed: {response.Error}");
            }

            var info = AnimationClipCommands.GetAnimationClip(PathRef(path));
            Assert.AreEqual(1, info.Bindings.Count);
            Assert.AreEqual(2, info.Bindings[0].KeyCount);
        }
    }
}
