using NUnit.Framework;
using UnityEditor;

namespace Unity.Pipeline.Tests.Editor
{
    /// <summary>
    /// Brackets the entire editor test run with "Enter Play Mode Options → Reload Domain DISABLED".
    ///
    /// Several fixtures drive <see cref="EditorApplication.isPlaying"/> directly. With domain reload
    /// enabled (the project default), entering play mode mid-test triggers an assembly reload the Test
    /// Runner did not schedule — it aborts with "Unexpected assembly reload happened while running
    /// tests" and never finishes. Disabling domain reload for the duration of the run lets those tests
    /// enter/exit play mode without reloading the domain (the live pipeline server also stays up).
    ///
    /// As a <see cref="SetUpFixtureAttribute"/> in the root test namespace, this applies to every test
    /// in the assembly (all sub-namespaces) and runs even when a single play-mode test is selected in
    /// the Test Runner window. The project's original Enter Play Mode settings are stashed in
    /// <see cref="SessionState"/> (which survives a domain reload, should one still slip through) and
    /// restored when the run ends.
    ///
    /// Note: this only governs the reload performed when ENTERING play mode. A test that explicitly
    /// recompiles (e.g. RequestScriptReload) would still reload — that is a different mechanism.
    /// </summary>
    [SetUpFixture]
    public class PlayModeReloadGuard
    {
        const string k_StashedKey = "Unity.Pipeline.Tests.EnterPlayModeStashed";
        const string k_EnabledKey = "Unity.Pipeline.Tests.EnterPlayModeEnabled";
        const string k_OptionsKey = "Unity.Pipeline.Tests.EnterPlayModeOptions";

        [OneTimeSetUp]
        public void DisableDomainReload()
        {
            // Stash the project's current settings once. Guard against overwriting a stash left behind
            // by a previous run that failed to restore (so the true original is never lost).
            if (SessionState.GetInt(k_StashedKey, 0) == 0)
            {
                SessionState.SetInt(k_StashedKey, 1);
                SessionState.SetInt(k_EnabledKey, EditorSettings.enterPlayModeOptionsEnabled ? 1 : 0);
                SessionState.SetInt(k_OptionsKey, (int)EditorSettings.enterPlayModeOptions);
            }

            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
        }

        [OneTimeTearDown]
        public void RestoreSettings()
        {
            if (SessionState.GetInt(k_StashedKey, 0) == 0)
                return;

            EditorSettings.enterPlayModeOptionsEnabled = SessionState.GetInt(k_EnabledKey, 0) == 1;
            EditorSettings.enterPlayModeOptions = (EnterPlayModeOptions)SessionState.GetInt(k_OptionsKey, 0);

            SessionState.EraseInt(k_StashedKey);
            SessionState.EraseInt(k_EnabledKey);
            SessionState.EraseInt(k_OptionsKey);
        }
    }
}
