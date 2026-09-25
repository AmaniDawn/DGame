using NUnit.Framework;
using Unity.Pipeline.Editor;

// No namespace on purpose: an NUnit [SetUpFixture] outside any namespace runs its OneTimeSetUp /
// OneTimeTearDown once for the WHOLE assembly, so this covers every editor test (including the
// Unity.Pipeline.Tests.Editor.ServerLifecyle sub-namespace).

/// <summary>
/// Disables the live editor server's self-healing watchdog for the duration of the editor test run,
/// then restores it afterwards. The watchdog re-opens a dead listener on a timer; without this guard
/// it could race tests that deliberately start/stop servers or disturb the live one, re-opening a
/// listener mid-test. Disarming it for the run keeps the watchdog from interfering with tests while
/// still protecting the live server during the normal dogfood loop.
/// </summary>
[SetUpFixture]
public sealed class PipelineWatchdogTestGuard
{
    private bool m_WasEnabled;
    private bool m_HadServer;

    [OneTimeSetUp]
    public void DisableWatchdog()
    {
        var server = PipelineServerStartup.Server;
        if (server == null)
            return;

        m_HadServer = true;
        m_WasEnabled = server.WatchdogEnabled;
        server.WatchdogEnabled = false;
    }

    [OneTimeTearDown]
    public void RestoreWatchdog()
    {
        if (!m_HadServer)
            return;

        // Re-read the live server: a domain reload during the run may have replaced the instance.
        var server = PipelineServerStartup.Server;
        if (server != null)
            server.WatchdogEnabled = m_WasEnabled;
    }
}
