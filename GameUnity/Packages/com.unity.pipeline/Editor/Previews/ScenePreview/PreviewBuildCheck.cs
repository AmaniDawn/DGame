#if UNITY_6000_0_OR_NEWER
using Unity.Pipeline.Config;
using Unity.Pipeline.Editor.BuildProcessors;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;

namespace Unity.Pipeline.Editor.Previews
{
    /// <summary>
    /// Fails a build of the preview build profile that does not define
    /// <see cref="ScenePreviewSettings.ScriptingDefine"/>, since that player would have no archive loader.
    /// </summary>
#if UNITY_6000_3_OR_NEWER
    class PreviewBuildCheck : IPreprocessBuildWithContext
#else
    class PreviewBuildCheck : IPreprocessBuildWithReport
#endif
    {
        public int callbackOrder => 0;

#if UNITY_6000_3_OR_NEWER
        public void OnPreprocessBuild(BuildCallbackContext ctx)
        {
            var report = ctx?.Report;
#else
        public void OnPreprocessBuild(BuildReport report)
        {
#endif
            var target = report == null
                ? PipelineBuildSymbols.ActiveTarget()
                : PipelineRuntimeBuildProcessor.TargetFor(report.summary);
            Check(ScenePreviewSettings.ActivePreviewBuildProfile(), PlayerSettings.GetScriptingDefineSymbols(target));
        }

        /// <param name="profile">The preview build profile when it is the one building, else null.</param>
        /// <param name="playerDefines">The Player Settings defines the build compiles with.</param>
        internal static void Check(BuildProfile profile, string playerDefines)
        {
            if (profile == null || ScenePreviewSettings.HasScriptingDefine(profile) ||
                PipelineBuildSymbols.Contains(playerDefines, ScenePreviewSettings.ScriptingDefine))
                return;

            throw new BuildFailedException(
                $"Scene Preview: the preview build profile '{AssetDatabase.GetAssetPath(profile)}' does not define " +
                $"{ScenePreviewSettings.ScriptingDefine}, so the player would have no archive loader. Add it to the " +
                "profile's Scripting Defines, or from Project Settings > Pipeline > Editor > Scene Preview.");
        }
    }
}
#endif
