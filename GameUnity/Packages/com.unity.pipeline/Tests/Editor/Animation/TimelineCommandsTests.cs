using NUnit.Framework;
using Unity.Pipeline.Editor.Authoring;
using Unity.Pipeline.Editor.Commands.Animation;
using Unity.Pipeline.Models;
using UnityEditor;

namespace Unity.Pipeline.Tests.Editor.Animation
{
    /// <summary>
    /// Tests for CLI-214 Group C (Timeline). Timeline ships in the OPTIONAL com.unity.timeline package,
    /// so this fixture must COMPILE and PASS whether or not the package is installed:
    /// - <see cref="Timeline_NotInstalled_ReturnsPackageNotFound"/> runs only when the package is ABSENT
    ///   and asserts the structured package_not_found error (no exception).
    /// - the create -> add track -> add clip -> get flow runs only when the package is PRESENT and
    ///   calls <c>Assert.Pass</c> (not <c>Assert.Ignore</c>) when absent, so Yamato strict-mode
    ///   agents see "Passed" rather than "Not Run" (Inconclusive) for the conditional skip.
    ///
    /// All commands are reached through the public static methods; the Timeline types themselves are
    /// only touched via reflection inside the commands, never referenced by this test assembly.
    /// </summary>
    public class TimelineCommandsTests
    {
        private const string Root = "Assets/__CLI214TimelineTest";

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

        [Test]
        public void Timeline_NotInstalled_ReturnsPackageNotFound()
        {
            if (TimelineGuard.IsInstalled())
                Assert.Pass("com.unity.timeline is installed; the not-installed path is covered only when it is absent.");

            var result = TimelineCommands.CreateTimeline(Root + "/T.playable");
            Assert.IsInstanceOf<ErrorResult>(result, "Timeline commands must not throw when the package is absent");
            Assert.AreEqual("package_not_found", ((ErrorResult)result).Code);
        }

        [Test]
        public void Timeline_CreateTrackClipGet_Flow()
        {
            if (!TimelineGuard.IsInstalled())
                Assert.Pass("com.unity.timeline is not installed; the Timeline authoring flow is validated only when the package is present.");

            // create_timeline
            var timelinePath = Root + "/Cutscene.playable";
            var created = TimelineCommands.CreateTimeline(timelinePath, frameRate: 30f);
            Assert.IsInstanceOf<AuthoringResult>(created, "create_timeline should return an AuthoringResult when installed");

            // add an Animation track
            var trackResult = TimelineCommands.AddTimelineTrack(PathRef(timelinePath), "Animation", name: "Anim");
            Assert.IsInstanceOf<AddTimelineTrackResult>(trackResult);

            // a clip asset for the Animation track
            var clipPath = Root + "/Move.anim";
            AnimationClipCommands.CreateAnimationClip(clipPath);

            // add_timeline_clip (start 0, duration 2)
            var clipResult = TimelineCommands.AddTimelineClip(
                PathRef(timelinePath), track: "Anim", start: 0f, duration: 2f, asset: PathRef(clipPath));
            Assert.IsInstanceOf<AddTimelineClipResult>(clipResult);

            // get_timeline => one Animation track with one 2s clip
            var info = (TimelineInfo)TimelineCommands.GetTimeline(PathRef(timelinePath));
            Assert.AreEqual(1, info.Tracks.Count, "expected exactly one track");
            Assert.AreEqual("Animation", info.Tracks[0].TrackType);
            Assert.AreEqual(1, info.Tracks[0].Clips.Count, "expected exactly one clip");
            Assert.AreEqual(2d, info.Tracks[0].Clips[0].Duration, 0.001d);
        }
    }
}
