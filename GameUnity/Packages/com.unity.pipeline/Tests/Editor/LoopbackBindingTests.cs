using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Pipeline.Security;

namespace Unity.Pipeline.Tests.Editor
{
    /// <summary>
    /// Verifies the server is reachable over the explicit IPv4 loopback (127.0.0.1), independent of
    /// how "localhost" happens to resolve. This guards the intermittent connectivity failures caused
    /// by "localhost" resolving to IPv6 (::1) on some hosts while the server bound only IPv4 (or vice
    /// versa). Note: Unity's Mono HttpListener cannot serve requests that arrive over the IPv6
    /// loopback (it mis-parses the "[::1]" host and returns 400), so IPv4 is the reliable channel.
    /// </summary>
    public class LoopbackBindingTests
    {
        private static int GetStatus(string url)
        {
            var task = Task.Run(async () =>
            {
                using (var client = new HttpClient())
                using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    request.Headers.TryAddWithoutValidation(
                        "Authorization", $"Bearer {SecurityTokenManager.GetOrCreateToken()}");

                    var response = await client.SendAsync(request);
                    return (int)response.StatusCode;
                }
            });

            return task.GetAwaiter().GetResult();
        }

        [Test]
        public void Status_OverIPv4Loopback_Returns200()
        {
            if (!Socket.OSSupportsIPv4)
                Assert.Ignore("IPv4 not available on this host.");

            using (var server = new PipelineTestServer())
                Assert.AreEqual(200, GetStatus($"http://127.0.0.1:{server.Port}/api/status"),
                    "Server should be reachable over the explicit IPv4 loopback.");
        }
    }
}
