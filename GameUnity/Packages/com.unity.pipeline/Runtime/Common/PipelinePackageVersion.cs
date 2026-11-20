namespace Unity.Pipeline
{
    /// <summary>
    /// The version of the com.unity.pipeline package this code ships in, as reported by
    /// /api/commands. The editor reads it from the Package Manager; a Player reads the value
    /// PipelineRuntimeBuildProcessor baked into <see cref="Config.RuntimePipelineBuildInfo"/>.
    /// </summary>
    internal static class PipelinePackageVersion
    {
        private static string s_Version;

        /// <summary>
        /// The package version, or null when it cannot be determined (a Player built without the
        /// build processor). Read it from the main thread the first time: both sources are
        /// main-thread-only. The value is cached afterwards.
        /// </summary>
        public static string Current => s_Version ??= Resolve();

        /// <summary>
        /// The version a Player reports: the one baked into its build info, or null when the build
        /// info is missing or carries none.
        /// </summary>
        /// <param name="buildInfo">The Player's build info, or null when it has none.</param>
        /// <returns>The baked package version, or null.</returns>
        internal static string FromBuildInfo(Config.RuntimePipelineBuildInfo buildInfo)
        {
            return buildInfo != null && !string.IsNullOrEmpty(buildInfo.packageVersion) ? buildInfo.packageVersion : null;
        }

        private static string Resolve()
        {
#if UNITY_EDITOR
            return UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(PipelinePackageVersion).Assembly)?.version;
#else
            return FromBuildInfo(Config.RuntimePipelineBuildInfo.Load());
#endif
        }
    }
}
