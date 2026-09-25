using System;
using System.Collections;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Unity.Pipeline.Tests.Runtime
{
    /// <summary>
    /// Unified client for making HTTP requests to Pipeline server in tests.
    /// Provides consistent JSON payload structure and response parsing.
    /// </summary>
    public class PipelineClient : IDisposable
    {
        private readonly string m_BaseUrl;
        private readonly string m_AuthToken;
        private HttpClient m_HttpClient;
        private bool m_Disposed = false;

        /// <summary>
        /// Create a Pipeline client for the specified server.
        /// </summary>
        /// <param name="baseUrl">Base URL of the Pipeline server (e.g., "http://localhost:7900")</param>
        /// <param name="authToken">Optional authentication token</param>
        public PipelineClient(string baseUrl, string authToken = null)
        {
            m_BaseUrl = baseUrl.TrimEnd('/');
            m_AuthToken = authToken;
        }

        /// <summary>
        /// Create a client for a running server, deriving the URL (port) and auth token from it.
        /// The server must be started so its token is available.
        /// </summary>
        public PipelineClient(BasePipelineServer server)
            : this($"http://localhost:{server.Port}", server.Token)
        {
        }

        /// <summary>
        /// Create a client for a runtime server owned by a <see cref="RuntimePipelineManager"/>.
        /// The manager must have started its server.
        /// </summary>
        public PipelineClient(RuntimePipelineManager manager)
            : this(manager.Server)
        {
        }

        /// <summary>
        /// Initialize HttpClient for async operations (lazy initialization).
        /// </summary>
        private HttpClient GetHttpClient()
        {
            if (m_HttpClient == null)
            {
                m_HttpClient = new HttpClient();
                if (!string.IsNullOrEmpty(m_AuthToken))
                {
                    m_HttpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", m_AuthToken);
                }
            }
            return m_HttpClient;
        }

        // Coroutine forms for [UnityTest]. They drive the async request by polling the Task each
        // frame (yield return null) rather than yielding a UnityWebRequest AsyncOperation: the
        // EditMode test runner does not await an AsyncOperation yielded from a nested enumerator,
        // and frame-pumping also keeps the editor/player update loop (and the server's main-thread
        // dispatcher) ticking so main-thread commands can complete.

        /// <summary>GET an endpoint (coroutine form for [UnityTest]).</summary>
        public IEnumerator GetAsync(string endpoint, System.Action<PipelineResponse> onComplete)
            => Await(GetAsync(endpoint), onComplete);

        /// <summary>Get server status via /api/status endpoint (coroutine form).</summary>
        public IEnumerator GetStatusAsync(System.Action<PipelineResponse> onComplete)
            => GetAsync("/api/status", onComplete);

        /// <summary>Execute a command via /api/exec endpoint (coroutine form).</summary>
        public IEnumerator ExecuteCommandAsync(string command, object parameters, System.Action<PipelineResponse> onComplete)
            => Await(ExecuteCommandAsync(command, parameters), onComplete);

        /// <summary>Execute a code evaluation command (coroutine form).</summary>
        public IEnumerator ExecuteCodeAsync(string code, int timeout, System.Action<PipelineResponse> onComplete)
            => Await(ExecuteCodeAsync(code, timeout), onComplete);

        /// <summary>Post JSON to an endpoint (coroutine form).</summary>
        public IEnumerator PostJsonAsync(string endpoint, object payload, System.Action<PipelineResponse> onComplete)
            => Await(PostJsonAsync(endpoint, payload), onComplete);

        private static IEnumerator Await(Task<PipelineResponse> task, System.Action<PipelineResponse> onComplete)
        {
            while (!task.IsCompleted)
                yield return null;
            onComplete?.Invoke(task.Result);
        }

        // ============================================================================
        // ASYNC METHODS (for Editor tests using async/await)
        // ============================================================================

        /// <summary>
        /// Get server status via /api/status endpoint (async version).
        /// </summary>
        public Task<PipelineResponse> GetStatusAsync() => GetAsync("/api/status");

        /// <summary>
        /// GET an endpoint (async version), returning a parsed <see cref="PipelineResponse"/>.
        /// </summary>
        public async Task<PipelineResponse> GetAsync(string endpoint)
        {
            var url = $"{m_BaseUrl}{endpoint}";

            try
            {
                var httpClient = GetHttpClient();
                var httpResponse = await httpClient.GetAsync(url);
                var content = await httpResponse.Content.ReadAsStringAsync();

                return CreateResponseFromHttp(httpResponse, content);
            }
            catch (Exception ex)
            {
                return new PipelineResponse
                {
                    IsSuccess = false,
                    Error = ex.Message,
                    RawResponse = ""
                };
            }
        }

        /// <summary>
        /// Execute a command via /api/exec endpoint (async version).
        /// </summary>
        public async Task<PipelineResponse> ExecuteCommandAsync(string command, object parameters)
        {
            var payload = new
            {
                command = command,
                parameters = parameters ?? new { }
            };

            return await PostJsonAsync("/api/exec", payload);
        }

        /// <summary>
        /// Execute a code evaluation command (async version).
        /// </summary>
        public async Task<PipelineResponse> ExecuteCodeAsync(string code, int timeout)
        {
            var parameters = new
            {
                code = code,
                timeout = timeout
            };

            return await ExecuteCommandAsync("eval", parameters);
        }

        /// <summary>
        /// Post JSON data to an endpoint (async version).
        /// </summary>
        public async Task<PipelineResponse> PostJsonAsync(string endpoint, object payload)
        {
            var url = $"{m_BaseUrl}{endpoint}";
            var jsonData = JsonConvert.SerializeObject(payload, Formatting.Indented);
            var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

            try
            {
                var httpClient = GetHttpClient();
                var httpResponse = await httpClient.PostAsync(url, content);
                var responseContent = await httpResponse.Content.ReadAsStringAsync();

                return CreateResponseFromHttp(httpResponse, responseContent);
            }
            catch (Exception ex)
            {
                return new PipelineResponse
                {
                    IsSuccess = false,
                    Error = ex.Message,
                    RawResponse = ""
                };
            }
        }

        /// <summary>
        /// Get raw HTTP response (async version).
        /// </summary>
        public async Task<HttpResponseMessage> GetHttpAsync(string endpoint)
        {
            var url = $"{m_BaseUrl}{endpoint}";
            var httpClient = GetHttpClient();
            return await httpClient.GetAsync(url);
        }

        // ============================================================================
        // SHARED HELPER METHODS
        // ============================================================================

        /// <summary>
        /// Create a PipelineResponse from HttpResponseMessage.
        /// </summary>
        private PipelineResponse CreateResponseFromHttp(HttpResponseMessage httpResponse, string content)
        {
            var response = new PipelineResponse
            {
                IsSuccess = httpResponse.IsSuccessStatusCode,
                StatusCode = (int)httpResponse.StatusCode,
                Error = httpResponse.IsSuccessStatusCode ? null : httpResponse.ReasonPhrase,
                RawResponse = content ?? "",
                HttpResponse = httpResponse
            };

            // Try to parse JSON response
            if (!string.IsNullOrEmpty(response.RawResponse))
            {
                try
                {
                    response.JsonResponse = JObject.Parse(response.RawResponse);
                }
                catch (JsonException ex)
                {
                    response.ParseError = ex.Message;
                }
            }

            return response;
        }

        /// <summary>
        /// Dispose of any resources.
        /// </summary>
        public void Dispose()
        {
            if (!m_Disposed)
            {
                m_HttpClient?.Dispose();
                m_Disposed = true;
            }
        }
    }

    /// <summary>
    /// Standardized response from Pipeline server.
    /// </summary>
    public class PipelineResponse
    {
        /// <summary>
        /// Whether the HTTP request succeeded.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// HTTP status code.
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// HTTP error message (if any).
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// Raw response text.
        /// </summary>
        public string RawResponse { get; set; }

        /// <summary>
        /// Parsed JSON response (null if parsing failed).
        /// </summary>
        public JObject JsonResponse { get; set; }

        /// <summary>
        /// JSON parsing error (if any).
        /// </summary>
        public string ParseError { get; set; }

        /// <summary>
        /// Original HttpResponseMessage (for advanced scenarios with async methods).
        /// </summary>
        public HttpResponseMessage HttpResponse { get; set; }

        /// <summary>
        /// Whether the response has valid JSON.
        /// </summary>
        public bool HasValidJson => JsonResponse != null && string.IsNullOrEmpty(ParseError);

        /// <summary>
        /// Get a typed response object from the JSON.
        /// </summary>
        public T GetTypedResponse<T>() where T : class
        {
            if (!HasValidJson || JsonResponse == null || !JsonResponse.ContainsKey("result"))
                return null;

            try
            {
                return JsonResponse["result"].ToObject<T>();
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// Get a JSON property value.
        /// </summary>
        public T GetProperty<T>(string propertyName, T defaultValue = default(T))
        {
            if (!HasValidJson)
                return defaultValue;

            var token = JsonResponse[propertyName];
            if (token == null)
                return defaultValue;

            try
            {
                return token.ToObject<T>();
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Check if the command execution was successful (different from HTTP success).
        /// </summary>
        public bool IsCommandSuccess => GetProperty("success", false);

        /// <summary>
        /// Get command error message.
        /// </summary>
        public string CommandError => GetProperty<string>("error");

        /// <summary>
        /// Get command error details.
        /// </summary>
        public string CommandErrorDetails => GetProperty<string>("errorDetails");

        public override string ToString()
        {
            var status = IsSuccess ? "Success" : "Failed";
            var content = HasValidJson ? "JSON" : "Raw";
            return $"{status} ({StatusCode}) - {content}: {RawResponse?.Substring(0, Math.Min(100, RawResponse.Length))}...";
        }
    }

    /// <summary>
    /// Extension methods for common Pipeline operations.
    /// </summary>
    public static class PipelineClientExtensions
    {
        /// <summary>
        /// Execute code evaluation and get strongly-typed response (coroutine version).
        /// </summary>
        public static IEnumerator ExecuteCodeWithResponseAsync(this PipelineClient client, string code, int timeout, System.Action<Unity.Pipeline.Models.EvalResponse> onComplete)
        {
            yield return client.ExecuteCodeAsync(code, timeout, response =>
            {
                if (response.HasValidJson)
                {
                    var evalResponse = response.GetTypedResponse<Unity.Pipeline.Models.EvalResponse>();
                    onComplete?.Invoke(evalResponse);
                }
                else
                {
                    // Create error response
                    var errorResponse = new Unity.Pipeline.Models.EvalResponse
                    {
                        Success = false,
                        Error = response.Error ?? "HTTP Error",
                        ErrorDetails = response.RawResponse ?? "No response data"
                    };
                    onComplete?.Invoke(errorResponse);
                }
            });
        }

        /// <summary>
        /// Execute code evaluation and get strongly-typed response (async version).
        /// </summary>
        public static async Task<Unity.Pipeline.Models.EvalResponse> ExecuteCodeWithResponseAsync(this PipelineClient client, string code, int timeout = 5000)
        {
            var response = await client.ExecuteCodeAsync(code, timeout);

            if (response.HasValidJson)
            {
                var evalResponse = response.GetTypedResponse<Unity.Pipeline.Models.EvalResponse>();
                if (evalResponse != null)
                    return evalResponse;
            }

            // Create error response if parsing failed
            return new Unity.Pipeline.Models.EvalResponse
            {
                Success = false,
                Error = response.Error ?? "HTTP Error",
                ErrorDetails = response.RawResponse ?? "No response data"
            };
        }
    }
}