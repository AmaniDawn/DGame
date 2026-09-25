using System.Net.Http;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Pipeline.Security;

namespace Unity.Pipeline.Tests.Editor
{
    /// <summary>
    /// Verifies the server-level authentication and CORS gate that applies to every route:
    /// a bearer token is required, and any request carrying an Origin header is rejected.
    /// </summary>
    public class ServerAuthTests
    {
        private static (int status, string body) Get(string url, string token = null, string origin = null)
        {
            var task = Task.Run(async () =>
            {
                using (var client = new HttpClient())
                using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    if (token != null)
                        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {token}");
                    if (origin != null)
                        request.Headers.TryAddWithoutValidation("Origin", origin);

                    var response = await client.SendAsync(request);
                    var body = await response.Content.ReadAsStringAsync();
                    return ((int)response.StatusCode, body);
                }
            });

            return task.GetAwaiter().GetResult();
        }

        private static (int status, string body) Post(string url, string jsonBody, string token = null)
        {
            var task = Task.Run(async () =>
            {
                using (var client = new HttpClient())
                using (var request = new HttpRequestMessage(HttpMethod.Post, url))
                {
                    if (token != null)
                        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {token}");
                    request.Content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json");

                    var response = await client.SendAsync(request);
                    var body = await response.Content.ReadAsStringAsync();
                    return ((int)response.StatusCode, body);
                }
            });

            return task.GetAwaiter().GetResult();
        }

        [Test]
        public void Status_WithoutToken_Returns401()
        {
            using (var server = new PipelineTestServer())
                Assert.AreEqual(401, Get($"http://localhost:{server.Port}/api/status").status);
        }

        [Test]
        public void Status_WithInvalidToken_Returns401()
        {
            using (var server = new PipelineTestServer())
                Assert.AreEqual(401, Get($"http://localhost:{server.Port}/api/status", token: "not-the-token").status);
        }

        [Test]
        public void Status_WithValidToken_Returns200()
        {
            using (var server = new PipelineTestServer())
                Assert.AreEqual(200, Get($"http://localhost:{server.Port}/api/status", token: SecurityTokenManager.GetOrCreateToken()).status);
        }

        [Test, Ignore("6.0 Issues")]
        public void Request_WithOriginHeader_Returns403()
        {
            using (var server = new PipelineTestServer())
            {
                var status = Get($"http://localhost:{server.Port}/api/status",
                    token: SecurityTokenManager.GetOrCreateToken(), origin: "http://evil.test").status;
                Assert.AreEqual(403, status);
            }
        }

        private const string ExecLogEditorBody = "{\"command\":\"log_editor\",\"parameters\":{\"message\":\"hi\"}}";

        [Test]
        public void Exec_WithoutToken_Returns401()
        {
            using (var server = new PipelineTestServer())
                Assert.AreEqual(401, Post($"http://localhost:{server.Port}/api/exec", ExecLogEditorBody).status);
        }

        [Test]
        public void Exec_WithInvalidToken_Returns401()
        {
            using (var server = new PipelineTestServer())
                Assert.AreEqual(401, Post($"http://localhost:{server.Port}/api/exec", ExecLogEditorBody, token: "not-the-token").status);
        }
    }
}
