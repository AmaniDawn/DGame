using System;
using Unity.Pipeline.Editor;
using Unity.Pipeline.Security;

namespace Unity.Pipeline.Tests.Editor
{
    /// <summary>
    /// Editor pipeline server for tests. Binds to an isolated port range (7850-7899) and never
    /// writes the shared instance descriptor, so it can never collide with or clobber the live
    /// editor server (port 7800, descriptor .unity-pipeline-port) that agents drive over HTTP.
    ///
    /// Because no descriptor is created, the token comes straight from SecurityTokenManager
    /// (the same source the descriptor would use), so token-gated commands still validate.
    /// </summary>
    internal sealed class TestEditorPipelineServer : EditorPipelineServer
    {
        protected override bool WritesDescriptor => false;

        protected override (int basePort, int maxPort) GetPortRange() => (7850, 7899);

        protected override string GetToken() => SecurityTokenManager.GetOrCreateToken();

        // The base ties /api/status readiness to the instance descriptor, which this server
        // intentionally never writes. It is genuinely running once Start() returns, so report ready.
        protected override object GetServerStatus() => new { status = "ready", lastHeartbeat = DateTime.UtcNow };
    }
}
