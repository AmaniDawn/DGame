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
    /// Tests for CLI-214 Group B (animator controllers): create controller (Base Layer, no states),
    /// add parameter (and duplicate => duplicate_parameter), add layer, add state (with motion + as
    /// default), add transition (with a matching condition), and the validation failures (missing
    /// parameter, mode/type mismatch). Assets are generated in-test and cleaned up.
    /// </summary>
    public class AnimatorControllerCommandsTests
    {
        private const string Root = "Assets/__CLI214CtrlTest";

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

        private string CreateController(string name)
        {
            var path = $"{Root}/{name}.controller";
            AnimatorControllerCommands.CreateAnimatorController(path);
            return path;
        }

        [Test]
        public void CreateController_HasOneBaseLayerNoStates()
        {
            var path = CreateController("Empty");

            var info = AnimatorControllerCommands.GetAnimatorController(PathRef(path));
            Assert.AreEqual(1, info.Layers.Count, "A new controller should have exactly one (Base) layer");
            Assert.AreEqual(0, info.Layers[0].States.Count, "Base Layer should start with no states");
            Assert.AreEqual(0, info.Parameters.Count);
        }

        [Test]
        public void AddParameter_Float_AndDuplicate()
        {
            var path = CreateController("Params");

            var added = AnimatorControllerCommands.AddAnimatorParameter(PathRef(path), "Speed", "Float", new JValue(0f));
            Assert.IsInstanceOf<AddParameterResult>(added);

            var info = AnimatorControllerCommands.GetAnimatorController(PathRef(path));
            Assert.AreEqual(1, info.Parameters.Count);
            Assert.AreEqual("Speed", info.Parameters[0].Name);
            Assert.AreEqual("Float", info.Parameters[0].ParamType);

            // Duplicate name => structured error, not exception.
            var dup = AnimatorControllerCommands.AddAnimatorParameter(PathRef(path), "Speed", "Float");
            Assert.IsInstanceOf<ErrorResult>(dup);
            Assert.AreEqual("duplicate_parameter", ((ErrorResult)dup).Code);
        }

        [Test]
        public void AddLayer_AppendsLayer()
        {
            var path = CreateController("Layers");

            var added = AnimatorControllerCommands.AddAnimatorLayer(PathRef(path), "Upper", weight: 0.5f, blendingMode: "Additive");
            Assert.IsInstanceOf<AddLayerResult>(added);
            Assert.AreEqual(1, ((AddLayerResult)added).Layer.Index);

            var info = AnimatorControllerCommands.GetAnimatorController(PathRef(path));
            Assert.AreEqual(2, info.Layers.Count);
            Assert.AreEqual("Upper", info.Layers[1].Name);
        }

        [Test]
        public void AddState_WithMotionAndDefault()
        {
            var path = CreateController("States");

            // A clip to use as the state motion.
            var clipPath = Root + "/Idle.anim";
            AnimationClipCommands.CreateAnimationClip(clipPath);

            var added = AnimatorControllerCommands.AddAnimatorState(
                PathRef(path), layer: null, name: "Idle", motion: PathRef(clipPath), isDefault: true);
            Assert.IsInstanceOf<AddStateResult>(added);

            var info = AnimatorControllerCommands.GetAnimatorController(PathRef(path));
            Assert.AreEqual(1, info.Layers[0].States.Count);
            var state = info.Layers[0].States[0];
            Assert.AreEqual("Idle", state.Name);
            Assert.IsTrue(state.IsDefault, "Idle should be the layer default");
            Assert.IsNotNull(state.Motion, "Idle should have a non-null motion");
            Assert.AreEqual("Idle", info.Layers[0].DefaultState);
        }

        [Test]
        public void AddState_UnknownLayerName_ReturnsLayerNotFound()
        {
            var path = CreateController("BadLayer");

            var result = AnimatorControllerCommands.AddAnimatorState(
                PathRef(path), layer: new JValue("NoSuchLayer"), name: "S");
            Assert.IsInstanceOf<ErrorResult>(result);
            Assert.AreEqual("layer_not_found", ((ErrorResult)result).Code);
        }

        [Test]
        public void AddTransition_WithCondition()
        {
            var path = CreateController("Transitions");
            AnimatorControllerCommands.AddAnimatorParameter(PathRef(path), "Speed", "Float", new JValue(0f));
            AnimatorControllerCommands.AddAnimatorState(PathRef(path), null, "Idle");
            AnimatorControllerCommands.AddAnimatorState(PathRef(path), null, "Run");

            var conditions = new JArray
            {
                new JObject { ["parameter"] = "Speed", ["mode"] = "Greater", ["threshold"] = 0.1f }
            };
            var added = AnimatorControllerCommands.AddAnimatorTransition(
                PathRef(path), null, "Idle", "Run", conditions);
            Assert.IsInstanceOf<AddTransitionResult>(added);
            Assert.AreEqual(1, ((AddTransitionResult)added).Transition.ConditionCount);

            var info = AnimatorControllerCommands.GetAnimatorController(PathRef(path));
            var transitions = info.Layers[0].Transitions;
            Assert.AreEqual(1, transitions.Count);
            Assert.AreEqual("Idle", transitions[0].From);
            Assert.AreEqual("Run", transitions[0].To);
            Assert.AreEqual(1, transitions[0].Conditions.Count);
            Assert.AreEqual("Speed", transitions[0].Conditions[0].Parameter);
            Assert.AreEqual("Greater", transitions[0].Conditions[0].Mode);
        }

        [Test]
        public void AddTransition_UnknownParameter_ThrowsAndWritesNothing()
        {
            var path = CreateController("BadParam");
            AnimatorControllerCommands.AddAnimatorState(PathRef(path), null, "Idle");
            AnimatorControllerCommands.AddAnimatorState(PathRef(path), null, "Run");

            var conditions = new JArray
            {
                new JObject { ["parameter"] = "Ghost", ["mode"] = "Greater", ["threshold"] = 0.1f }
            };

            var ex = Assert.Throws<System.ArgumentException>(
                () => AnimatorControllerCommands.AddAnimatorTransition(PathRef(path), null, "Idle", "Run", conditions));
            StringAssert.Contains("Ghost", ex.Message, "error should name the offending parameter");

            var info = AnimatorControllerCommands.GetAnimatorController(PathRef(path));
            Assert.AreEqual(0, info.Layers[0].Transitions.Count, "nothing should be written on a bad condition");
        }

        [Test]
        public void AddTransition_ModeTypeMismatch_Throws()
        {
            var path = CreateController("Mismatch");
            AnimatorControllerCommands.AddAnimatorParameter(PathRef(path), "Speed", "Float", new JValue(0f));
            AnimatorControllerCommands.AddAnimatorState(PathRef(path), null, "Idle");
            AnimatorControllerCommands.AddAnimatorState(PathRef(path), null, "Run");

            // "If" is only valid for Bool/Trigger, not a Float parameter.
            var conditions = new JArray
            {
                new JObject { ["parameter"] = "Speed", ["mode"] = "If" }
            };

            Assert.Throws<System.ArgumentException>(
                () => AnimatorControllerCommands.AddAnimatorTransition(PathRef(path), null, "Idle", "Run", conditions));
        }

        [Test]
        public void AddState_FractionalLayerIndex_ReturnsLayerNotFound()
        {
            var path = CreateController("FractionalLayer");

            // 0.9 must NOT be silently rounded to layer 1; a non-integer index is rejected.
            var result = AnimatorControllerCommands.AddAnimatorState(
                PathRef(path), layer: new JValue(0.9d), name: "S");
            Assert.IsInstanceOf<ErrorResult>(result);
            Assert.AreEqual("layer_not_found", ((ErrorResult)result).Code);
        }

        [Test]
        public void AddState_IntegerValuedFloatLayer_Resolves()
        {
            var path = CreateController("IntFloatLayer");

            // 0.0 is an integer-valued float and resolves to the Base Layer (index 0).
            var added = AnimatorControllerCommands.AddAnimatorState(
                PathRef(path), layer: new JValue(0d), name: "Idle");
            Assert.IsInstanceOf<AddStateResult>(added);
            Assert.AreEqual(0, ((AddStateResult)added).State.Layer);
        }

        [Test]
        public void AddTransition_ExitTimeOutOfRange_Throws()
        {
            var path = CreateController("BadExitTime");
            AnimatorControllerCommands.AddAnimatorState(PathRef(path), null, "Idle");
            AnimatorControllerCommands.AddAnimatorState(PathRef(path), null, "Run");

            Assert.Throws<System.ArgumentException>(
                () => AnimatorControllerCommands.AddAnimatorTransition(
                    PathRef(path), null, "Idle", "Run", conditions: null, hasExitTime: true, exitTime: 1.5f));

            var info = AnimatorControllerCommands.GetAnimatorController(PathRef(path));
            Assert.AreEqual(0, info.Layers[0].Transitions.Count, "an out-of-range exitTime must write nothing");
        }

        [Test]
        public void AddTransition_NegativeDuration_Throws()
        {
            var path = CreateController("BadDuration");
            AnimatorControllerCommands.AddAnimatorState(PathRef(path), null, "Idle");
            AnimatorControllerCommands.AddAnimatorState(PathRef(path), null, "Run");

            Assert.Throws<System.ArgumentException>(
                () => AnimatorControllerCommands.AddAnimatorTransition(
                    PathRef(path), null, "Idle", "Run", conditions: null, duration: -1f));

            var info = AnimatorControllerCommands.GetAnimatorController(PathRef(path));
            Assert.AreEqual(0, info.Layers[0].Transitions.Count, "a negative duration must write nothing");
        }

        [Test]
        public void AddParameter_DryRun_WritesNothing()
        {
            var path = CreateController("DryParam");

            AnimatorControllerCommands.AddAnimatorParameter(PathRef(path), "Speed", "Float", new JValue(0f), dryRun: true);

            var info = AnimatorControllerCommands.GetAnimatorController(PathRef(path));
            Assert.AreEqual(0, info.Parameters.Count, "dry_run must not write a parameter");
        }

        [Test]
        public void CreateController_ViaClient_Succeeds()
        {
            using (var server = new PipelineTestServer())
            {
                var path = Root + "/ViaClient/Made.controller";
                var response = server.Execute("create_animator_controller", new { path });

                Assert.IsTrue(response.IsSuccess, $"create_animator_controller should succeed: {response.Error}");
                Assert.IsNotNull(AssetDatabase.LoadMainAssetAtPath(path), "Controller should exist on disk");
            }
        }
    }
}
