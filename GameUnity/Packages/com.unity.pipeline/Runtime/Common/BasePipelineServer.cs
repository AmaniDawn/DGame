using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Unity.Pipeline.Models;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Security;
using Unity.Pipeline.Threading;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Unity.Pipeline
{
    /// <summary>
    /// HTTP server that enables CLI tools to execute commands in Unity Editor.
    /// Represents an Instance that can serve as a Pipeline Element for automation.
    /// </summary>
    public abstract class BasePipelineServer
    {
        /// <summary>
        /// Maximum accepted request body size (1 MiB). Requests larger than this are rejected with
        /// 413 to bound the memory a single remote request can force the server to buffer.
        /// </summary>
        private const long MaxRequestBodyBytes = 1 * 1024 * 1024;

        /// <summary>
        /// Upper bound on how many bytes we read-and-discard from an over-limit request body before
        /// sending the 413. HttpListener resets the TCP connection if we close the response while the
        /// client is still uploading, and the client then sees a connection error instead of the 413.
        /// Draining lets the client finish so it can read the response. Kept comfortably above
        /// <see cref="MaxRequestBodyBytes"/> to cover clients only slightly over the cap, while still
        /// bounding the work a single abusive request can force (a larger body simply resets).
        /// </summary>
        private const long MaxDrainBytes = 8 * 1024 * 1024;

        private bool m_IsRunning;
        private int m_Port;
        private HttpListener m_HttpListener;
        private readonly Dispatcher m_Dispatcher = new Dispatcher();

        private bool m_WatchdogEnabled;
        private bool m_WatchdogArmed;
        private DateTime m_LastWatchdogCheck;

        /// <summary>
        /// This server's own main-thread dispatcher. Each server instance owns one (no global
        /// singleton), so tests that start their own server never affect any other server's
        /// dispatch. Pump it via ProcessWorkQueue from the main thread (auto-pumped on
        /// EditorApplication.update in the editor; pumped by RuntimePipelineManager.Update in a
        /// player; pumped explicitly by tests).
        /// </summary>
        public Dispatcher Dispatcher => m_Dispatcher;

        /// <summary>
        /// Whether the server is running AND its HTTP listener is actually listening. More accurate
        /// than the internal running flag alone: returns false if the listener was stopped/disposed
        /// (e.g. by a domain reload) even when the flag is stale-true. Cheap and non-blocking.
        ///
        /// A self-HTTP probe was considered for "does it actually respond" but rejected: the server
        /// processes requests strictly one-at-a-time (HandleRequests awaits ProcessRequest), so a
        /// self-probe deadlocks if issued from within a handler and false-negatives whenever a
        /// request is in flight — unsafe for a watchdog. IsListening reliably catches the realistic
        /// failure (listener stopped by a domain reload), which is what we need.
        /// </summary>
        public bool IsRunning => m_IsRunning && m_HttpListener != null && m_HttpListener.IsListening;

        /// <summary>
        /// When enabled, the server periodically checks its own HTTP listener and re-opens it in
        /// place if it died without going through <see cref="Stop"/> (e.g. an unexpected listener
        /// fault). The server instance survives such failures — only the listener dies — so it can
        /// self-heal without an external restart. Default OFF: transient/test servers must not
        /// watchdog. The editor owner enables it for the live server.
        ///
        /// Setting this while the server is running arms/disarms the watchdog immediately; otherwise
        /// it takes effect on the next <see cref="Start"/>. Domain reloads are NOT handled here (the
        /// instance is torn down and recreated by [InitializeOnLoad]); the watchdog only revives a
        /// listener that died while the instance is still alive.
        /// </summary>
        public bool WatchdogEnabled
        {
            get => m_WatchdogEnabled;
            set
            {
                if (m_WatchdogEnabled == value)
                    return;
                m_WatchdogEnabled = value;
                if (!m_IsRunning)
                    return;
                if (value)
                    ArmWatchdog();
                else
                    DisarmWatchdog();
            }
        }

        /// <summary>
        /// How often the watchdog checks the listener (seconds). Default 5.
        /// </summary>
        public double WatchdogIntervalSeconds { get; set; } = 5.0;

        /// <summary>
        /// Port number the server is listening on. 0 if not running.
        /// Range: 7800-7899 for Editor, 7900-7999 for Runtime (avoids unity-tools port range 37800-37899).
        /// </summary>
        public int Port => m_Port;

        public abstract DateTime StartedAt { get; }

        /// <summary>
        /// Whether this server writes/deletes the shared instance descriptor (.unity-pipeline-port).
        /// Test servers override this to false so they never clobber the live server's descriptor —
        /// the test already knows its port, so no discovery file is needed.
        /// </summary>
        protected virtual bool WritesDescriptor => true;

        /// <summary>
        /// Whether this server advertises commands marked [CliCommand(RuntimeOnly = true)] in
        /// its /api/commands listing. Runtime servers list them; Editor servers hide them so a
        /// client connected to an Editor only sees the Editor command surface.
        /// </summary>
        protected virtual bool IncludeRuntimeOnlyCommands => true;

        protected abstract void CreateInstanceDescriptor();
        protected abstract void DeleteInstanceDescriptor();
        protected abstract void UpdateHeartBeat();
        protected abstract object GetServerStatus();
        protected abstract string GetToken();

        protected virtual void ServerStarted()
        {

        }

        protected virtual void ServerStopped()
        {

        }

        /// <summary>
        /// This server's current auth token. Exposed to the test assembly (via InternalsVisibleTo)
        /// so the test client can authenticate without re-deriving the token.
        /// </summary>
        internal string Token => GetToken();

        /// <summary>
        /// Start the HTTP server on the specified port or auto-assign from range.
        /// </summary>
        /// <param name="port">Port to bind to, or 0 for auto-assignment from server-specific range</param>
        public void Start(int port = 0)
        {
            if (m_IsRunning)
                return;

            m_Port = port == 0 ? FindAvailablePort() : port;

            try
            {
                // Initialize this server's own dispatcher (captures the main thread; Start() is
                // always called from the main thread).
                m_Dispatcher.Initialize();

                // Mark running before opening the listener so HandleRequests' loop guard stays true
                // as soon as it starts on the threadpool.
                m_IsRunning = true;
                OpenListener();

                System.Console.WriteLine($"Start HTTP server: port:{m_Port}");

                if (WritesDescriptor)
                    CreateInstanceDescriptor();

                ArmWatchdog();

                // Keep the Editor and player loop (and thus Update -> Dispatcher.ProcessWorkQueue) running
                // while the window is unfocused or minimized this is similar to auto_tick (especially for the Player).
                Application.runInBackground = true;
                ServerStarted();
            }
            catch (Exception)
            {
                m_IsRunning = false;
                m_HttpListener?.Stop();
                m_HttpListener = null;
                throw;
            }
        }

        /// <summary>
        /// Create the HTTP listener, bind it, and start the request-handling loop. Extracted from
        /// <see cref="Start"/> so the watchdog can re-open a dead listener in place without
        /// re-running the rest of startup (dispatcher init, descriptor). Caller sets m_IsRunning.
        /// </summary>
        private void OpenListener()
        {
            m_HttpListener = new HttpListener();
            AddLoopbackPrefixes(m_HttpListener, m_Port);
            m_HttpListener.Start();

            // Start request handling
            _ = Task.Run(HandleRequests);
        }

        /// <summary>
        /// Bind a wildcard-host prefix so the listener accepts any Host header ("127.0.0.1",
        /// "localhost", "::1") no matter how a client — or its DNS resolver — resolves the name.
        /// This is required because Mono's HttpListener binds a hostname prefix to a single resolved
        /// address family and then matches the request's Host <em>literally</em> per prefix: an
        /// explicit "http://127.0.0.1/" prefix rejects "Host: localhost" (and vice-versa) with a
        /// 400. A wildcard sidesteps that. The wildcard binds all interfaces at the socket level, so
        /// access is confined to the local machine by the loopback check in
        /// <see cref="ProcessRequest"/> (plus bearer-token auth).
        /// </summary>
        private static void AddLoopbackPrefixes(HttpListener listener, int port)
        {
            listener.Prefixes.Add($"http://+:{port}/");
        }

        /// <summary>
        /// Stop the HTTP server and clean up resources.
        /// </summary>
        public void Stop()
        {
            if (!m_IsRunning)
                return;

            m_IsRunning = false;

            DisarmWatchdog();
            System.Console.WriteLine("Pipeline Server stopped");

            try
            {
                if (WritesDescriptor)
                    DeleteInstanceDescriptor();

                m_HttpListener?.Stop();
                m_HttpListener?.Close();

                ServerStopped();
            }
            catch
            {
                // Ignore cleanup errors
            }
            finally
            {
                m_HttpListener = null;
                m_Dispatcher.Shutdown();
            }
            // Note: Keep port value for diagnostic purposes
        }

        /// <summary>
        /// Arm the watchdog (no-op if disabled or already armed). In the editor the tick rides
        /// EditorApplication.update; in a player it is driven by RuntimePipelineManager.Update via
        /// <see cref="WatchdogTick"/>.
        /// </summary>
        private void ArmWatchdog()
        {
            if (!m_WatchdogEnabled || m_WatchdogArmed)
                return;

            m_WatchdogArmed = true;
            m_LastWatchdogCheck = DateTime.UtcNow;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.update += WatchdogTick;
#endif
        }

        private void DisarmWatchdog()
        {
            if (!m_WatchdogArmed)
                return;

            m_WatchdogArmed = false;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.update -= WatchdogTick;
#endif
        }

        /// <summary>
        /// Watchdog heartbeat: throttled to <see cref="WatchdogIntervalSeconds"/>, re-opens the HTTP
        /// listener in place if it died while the server is still meant to be running. Safe to call
        /// every frame. In the editor it is subscribed to EditorApplication.update by ArmWatchdog;
        /// in a player RuntimePipelineManager.Update calls it.
        /// </summary>
        public void WatchdogTick()
        {
            if (!m_WatchdogArmed)
                return;

#if UNITY_EDITOR
            // Don't fight domain reloads / asset updates — the listener is expected to be torn down
            // then, and [InitializeOnLoad] recreates the server afterwards.
            if (UnityEditor.EditorApplication.isCompiling || UnityEditor.EditorApplication.isUpdating)
                return;
#endif

            var now = DateTime.UtcNow;
            if ((now - m_LastWatchdogCheck).TotalSeconds < WatchdogIntervalSeconds)
                return;
            m_LastWatchdogCheck = now;

            if (m_HttpListener != null && m_HttpListener.IsListening)
                return; // healthy

            try
            {
                // Dispose the dead listener so it releases the port binding before we re-bind a new
                // one on the same port (a stopped-but-not-closed listener can keep the port held).
                try { m_HttpListener?.Close(); } catch { }
                m_HttpListener = null;

                // The instance is alive and the watchdog is armed → the server is meant to be
                // listening. Re-open the listener in place.
                m_IsRunning = true;
                OpenListener();
                Debug.Log($"Pipeline watchdog re-opened HTTP listener on port {m_Port}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Pipeline watchdog failed to re-open listener on port {m_Port}: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle incoming HTTP requests with routing to appropriate endpoints.
        /// </summary>
        private async Task HandleRequests()
        {
            while (m_IsRunning && m_HttpListener != null && m_HttpListener.IsListening)
            {
                try
                {
                    var context = await m_HttpListener.GetContextAsync();
                    await ProcessRequest(context);
                }
                catch (ObjectDisposedException)
                {
                    // Listener was stopped, exit gracefully
                    break;
                }
                catch (HttpListenerException)
                {
                    // Listener was stopped, exit gracefully
                    break;
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex);
                }
            }
            m_IsRunning = false;
        }

        /// <summary>
        /// Process individual HTTP request and route to appropriate handler.
        /// </summary>
        protected virtual async Task ProcessRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            try
            {
                // Confine access to the local machine. The listener binds a wildcard host (see
                // AddLoopbackPrefixes) so it accepts any Host header, which also means it accepts
                // connections on non-loopback interfaces — reject anything that isn't loopback
                // before doing any other work. Bearer-token auth is still enforced below.
                var remoteAddress = request.RemoteEndPoint?.Address;
                if (remoteAddress == null || !IPAddress.IsLoopback(remoteAddress))
                {
                    await SendStatusResponse(response, 403, "Forbidden", "Only loopback connections are allowed");
                    return;
                }

                // Reject browser-originated requests. Legitimate non-browser clients (CLI, CI) never
                // send an Origin header; emitting no CORS headers and refusing any request that
                // carries one prevents a website in the developer's browser from reaching this
                // local server (and short-circuits CORS preflights).
                if (!string.IsNullOrEmpty(request.Headers["Origin"]))
                {
                    await SendStatusResponse(response, 403, "Forbidden", "Cross-origin requests are not allowed");
                    return;
                }

                // Authenticate every request with the bearer token before routing.
                if (!IsAuthorized(request))
                {
                    await SendStatusResponse(response, 401, "Unauthorized", "Missing or invalid authentication token");
                    return;
                }

                // Route to appropriate endpoint
                var path = request.Url.AbsolutePath.ToLowerInvariant();

                switch (path)
                {
                    case "/api/status":
                        await HandleStatusRequest(response);
                        break;
                    case "/api/editor_status":
                        await HandleEditorStatusRequest(response);
                        break;
                    case "/api/commands":
                        await HandleCommandsRequest(response);
                        break;
                    case "/api/exec":
                        if (request.HttpMethod == "POST")
                            await HandleExecRequest(request, response);
                        else
                            await HandleMethodNotAllowed(response);
                        break;
                    case "/api/test-status":
                        await HandleTestStatusRequest(response);
                        break;
                    default:
                        await HandleNotFound(response);
                        break;
                }
            }
            catch (Exception)
            {
                // Ensure response is always closed
                try
                {
                    if (response.OutputStream.CanWrite)
                    {
                        response.StatusCode = 500;
                        response.OutputStream.Close();
                    }
                }
                catch { }
            }
        }

        /// <summary>
        /// Validate the request's bearer token against this server's token.
        /// </summary>
        private bool IsAuthorized(HttpListenerRequest request)
        {
            var header = request.Headers["Authorization"];
            if (string.IsNullOrEmpty(header))
                return false;

            const string prefix = "Bearer ";
            if (!header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return false;

            var token = header.Substring(prefix.Length).Trim();
            return SecurityTokenManager.ConstantTimeEquals(token, GetToken());
        }

        /// <summary>
        /// Send a structured JSON error response with an explicit HTTP status code.
        /// </summary>
        private async Task SendStatusResponse(HttpListenerResponse response, int statusCode, string error, string details)
        {
            try
            {
                response.StatusCode = statusCode;
                response.ContentType = "application/json";
                var json = JsonConvert.SerializeObject(BaseResponse.Failure(error, details), Formatting.Indented);
                var buffer = Encoding.UTF8.GetBytes(json);
                response.ContentLength64 = buffer.Length;

                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            }
            catch
            {
                try { response.OutputStream.Close(); } catch { }
            }
        }

        /// <summary>
        /// Handle /api/status endpoint - returns basic server health information.
        /// No Editor API access required, always fast response.
        /// </summary>
        private async Task HandleStatusRequest(HttpListenerResponse response)
        {
            try
            {
                var basicStatus = GetServerStatus();
                var json = JsonConvert.SerializeObject(basicStatus, Formatting.Indented);

                response.StatusCode = 200;
                response.ContentType = "application/json";

                var buffer = Encoding.UTF8.GetBytes(json);
                response.ContentLength64 = buffer.Length;

                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            }
            catch (Exception ex)
            {
                Debug.LogError($"HandleStatusRequest failed: {ex.Message}");
                await SendErrorResponse(response, "Status Error", $"Failed to get status: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle /api/editor_status endpoint - returns detailed Editor state via command execution.
        /// Executes the "editor_status" command to get rich Editor information.
        /// </summary>
        private async Task HandleEditorStatusRequest(HttpListenerResponse response)
        {
            try
            {
                // TODO: should this be implemented as a command??

                // Execute editor_status command to get rich Editor information
                var result = await ExecuteCommandByName("editor_status", new JObject());

                // The editor_status command returns a StatusResponse directly
                var editorStatus = result as StatusResponse;
                if (editorStatus != null)
                {
                    // Update with server-specific information
                    UpdateStatusWithServerInfo(editorStatus);

                    var json = JsonConvert.SerializeObject(editorStatus, Formatting.Indented);

                    response.StatusCode = 200;
                    response.ContentType = "application/json";

                    var buffer = Encoding.UTF8.GetBytes(json);
                    response.ContentLength64 = buffer.Length;

                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    response.OutputStream.Close();
                }
                else
                {
                    Debug.LogError($"HandleEditorStatusRequest: editor_status command returned wrong type: {result?.GetType().FullName ?? "null"}");
                    await SendErrorResponse(response, "Editor Status Error", "editor_status command did not return valid StatusResponse");
                }
            }
            catch (Exception ex)
            {
                var errorMessage = !string.IsNullOrEmpty(ex.Message) ? ex.Message : "[EMPTY EXCEPTION MESSAGE]";
                Debug.LogError($"HandleEditorStatusRequest failed:");
                Debug.LogError($"  Exception Type: {ex.GetType().FullName}");
                Debug.LogError($"  Message: '{errorMessage}'");
                Debug.LogError($"  Full Exception: {ex}");

                await SendErrorResponse(response, "Editor Status Error", $"Failed to get editor status: {errorMessage}");
            }
        }

        /// <summary>
        /// Update StatusResponse with server-specific information.
        /// Used by /api/editor_status to add server metadata to command result.
        /// </summary>
        private void UpdateStatusWithServerInfo(StatusResponse editorStatus)
        {
            UpdateHeartBeat();
            // Ensure heartbeat is current
            editorStatus.LastHeartbeat = DateTime.UtcNow;
        }

        /// <summary>
        /// Handle /api/commands endpoint - returns available CLI commands with schemas.
        /// </summary>
        private async Task HandleCommandsRequest(HttpListenerResponse response)
        {
            try
            {
                var commands = CommandRegistry.DiscoverCommands()
                    .Where(c => IncludeRuntimeOnlyCommands || !c.RuntimeOnly)
                    .ToList();
                var commandsJson = commands.Select(BuildCommandResponse).ToList();

                var responseData = new
                {
                    commands = commandsJson,
                    count = commands.Count,
                    server = new
                    {
                        version = "0.0.1", // TODO: Get from package.json
                        port = m_Port,
                        startTime = StartedAt
                    }
                };

                var json = JsonConvert.SerializeObject(responseData, Formatting.Indented);

                response.StatusCode = 200;
                response.ContentType = "application/json";

                var buffer = Encoding.UTF8.GetBytes(json);
                response.ContentLength64 = buffer.Length;

                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to handle /api/commands request: {ex.Message}");

                // Return error response
                response.StatusCode = 500;
                response.ContentType = "application/json";

                var errorResponse = new { error = "Internal server error", message = ex.Message };
                var errorJson = JsonConvert.SerializeObject(errorResponse);
                var errorBuffer = Encoding.UTF8.GetBytes(errorJson);
                response.ContentLength64 = errorBuffer.Length;

                await response.OutputStream.WriteAsync(errorBuffer, 0, errorBuffer.Length);
                response.OutputStream.Close();
            }
        }

        /// <summary>
        /// Build JSON response object for a single command.
        /// </summary>
        private object BuildCommandResponse(CommandInfo command)
        {
            return new
            {
                name = command.Name,
                description = command.Description,
                mainThreadRequired = command.MainThreadRequired,
                runtimeOnly = command.RuntimeOnly,
                parameters = command.Parameters.Select(p => new
                {
                    name = p.Name,
                    description = p.Description,
                    type = p.ParameterType.Name,
                    required = p.Required,
                    defaultValue = p.DefaultValue
                }).ToList(),
                schema = JsonSchemaGenerator.GenerateCommandSchema(command)
            };
        }

        /// <summary>
        /// Handle 404 Not Found responses.
        /// </summary>
        private async Task HandleNotFound(HttpListenerResponse response)
        {
            response.StatusCode = 404;
            response.ContentType = "text/plain";

            var responseText = "Not Found";
            var buffer = Encoding.UTF8.GetBytes(responseText);
            response.ContentLength64 = buffer.Length;

            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }

        /// <summary>
        /// Handle 405 Method Not Allowed responses.
        /// </summary>
        private async Task HandleMethodNotAllowed(HttpListenerResponse response)
        {
            await SendErrorResponse(response, "Method Not Allowed", "This endpoint only supports POST requests");
        }

        /// <summary>
        /// Handle /api/test-status endpoint - returns status of async test execution.
        /// </summary>
        private async Task HandleTestStatusRequest(HttpListenerResponse response)
        {
            try
            {
                // Execute test_status command to get current test status
                var result = await ExecuteCommandByName("test_status", new JObject());

                string jsonResponse;
                if (result is string statusString)
                {
                    // test_status command returns JSON string directly
                    jsonResponse = statusString;
                }
                else
                {
                    // Fallback - serialize whatever was returned
                    jsonResponse = JsonConvert.SerializeObject(result ?? new { status = "no_tests", message = "No test run in progress" });
                }

                response.StatusCode = 200;
                response.ContentType = "application/json";

                var buffer = Encoding.UTF8.GetBytes(jsonResponse);
                response.ContentLength64 = buffer.Length;

                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            }
            catch (Exception ex)
            {
                Debug.LogError($"HandleTestStatusRequest failed: {ex.Message}");
                await SendErrorResponse(response, "Test Status Error", $"Failed to get test status: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle /api/exec endpoint - execute CLI commands with parameters.
        /// </summary>
        private async Task HandleExecRequest(HttpListenerRequest request, HttpListenerResponse response)
        {
            var cmd = "";
            string requestBody = null;
            try
            {
                // Reject oversized bodies up front via Content-Length (cheap, before reading anything).
                if (request.ContentLength64 > MaxRequestBodyBytes)
                {
                    // Drain the body first so the client can read the 413 (see DrainRequestBody).
                    await DrainRequestBody(request, MaxDrainBytes);
                    await SendExecResponse(response, 413,
                        BaseResponse.Failure("Payload Too Large",
                            $"Request body exceeds the maximum allowed size of {MaxRequestBodyBytes} bytes"), null);
                    return;
                }

                // Read request body, enforcing the same cap while reading in case Content-Length is
                // absent or untruthful (e.g. chunked transfer-encoding).
                try
                {
                    // leaveOpen: on an over-limit read we still need the raw InputStream open to drain
                    // the unsent remainder before responding, so it must outlive the reader/wrapper.
                    using (var limited = new MaxLengthStream(request.InputStream, MaxRequestBodyBytes, leaveOpen: true))
                    using (var reader = new StreamReader(limited))
                    {
                        requestBody = await reader.ReadToEndAsync();
                    }
                }
                catch (RequestTooLargeException ex)
                {
                    // Drain the body first so the client can read the 413 (see DrainRequestBody).
                    await DrainRequestBody(request, MaxDrainBytes);
                    await SendExecResponse(response, 413,
                        BaseResponse.Failure("Payload Too Large", ex.Message), null);
                    return;
                }
                finally
                {
                    // MaxLengthStream left the InputStream open; dispose it now that any read/drain is done.
                    request.InputStream.Dispose();
                }

                if (string.IsNullOrEmpty(requestBody))
                {
                    await SendExecResponse(response, 400,
                        BaseResponse.Failure("Bad Request", "Request body is required"), requestBody);
                    return;
                }

                // Parse command request
                CommandExecutionRequest commandRequest;
                try
                {
                    commandRequest = JsonConvert.DeserializeObject<CommandExecutionRequest>(requestBody);
                }
                catch (JsonException ex)
                {
                    await SendExecResponse(response, 400,
                        BaseResponse.Failure("Invalid JSON", $"Failed to parse request body: {ex.Message}"), requestBody);
                    return;
                }

                // Validate request structure
                var requestValidationError = commandRequest?.Validate();
                if (!string.IsNullOrEmpty(requestValidationError))
                {
                    await SendExecResponse(response, 400,
                        BaseResponse.Failure("Invalid Request", requestValidationError), requestBody);
                    return;
                }

                cmd = commandRequest.Command;
                // Execute command using shared execution logic
                var result = await ExecuteCommandByName(commandRequest.Command, commandRequest.Parameters);

                // Send success response
                await SendExecResponse(response, 200,
                    CommandExecutionResponse.CmdSuccess(commandRequest.Command, result), requestBody);
            }
            catch (ArgumentException ex)
            {
                // Parameter validation errors
                await SendExecResponse(response, 400,
                    CommandExecutionResponse.CmdFailure(cmd, "Parameter Validation Failed", ex.Message), requestBody);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("No command named"))
            {
                // Command not found errors
                await SendExecResponse(response, 400,
                    CommandExecutionResponse.CmdFailure(cmd, "Command Not Found", ex.Message), requestBody);
            }
            catch (InvalidOperationException ex)
            {
                // Command execution errors
                await SendExecResponse(response, 400,
                    CommandExecutionResponse.CmdFailure(cmd, "Command Execution Failed", ex.Message), requestBody);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to handle /api/exec request: {ex.Message}");
                await SendExecResponse(response, 400,
                    CommandExecutionResponse.CmdFailure(cmd, "Internal Server Error", ex.Message), requestBody);
            }
        }

        /// <summary>
        /// Read and discard the remaining request body, up to <paramref name="maxDrainBytes"/> bytes.
        /// When we reject a request early (e.g. 413) the client may still be uploading; if we close the
        /// response while unread data is in flight, HttpListener resets the TCP connection and the client
        /// sees a connection error instead of our HTTP response. Draining lets the upload complete so the
        /// response is delivered. Best-effort and bounded: a body larger than the budget stops draining
        /// (and may reset), which is acceptable for an abusive request.
        /// </summary>
        private static async Task DrainRequestBody(HttpListenerRequest request, long maxDrainBytes)
        {
            try
            {
                var input = request.InputStream;
                var buffer = new byte[16 * 1024];
                long drained = 0;
                while (drained < maxDrainBytes)
                {
                    var read = await input.ReadAsync(buffer, 0, buffer.Length);
                    if (read <= 0)
                        break;
                    drained += read;
                }
            }
            catch
            {
                // Best-effort: if the client already closed the connection or the stream is gone,
                // there is nothing left to drain and the response send below will handle the rest.
            }
        }

        /// <summary>
        /// Send error response with structured JSON format.
        /// </summary>
        private async Task SendErrorResponse(HttpListenerResponse response, string error, string details = null)
        {
            try
            {
                var errorResponse = BaseResponse.Failure(error, details);
                await SendResponse(response, 400, errorResponse);
            }
            catch
            {
                // Ignore errors in error handling
            }
        }

        private async Task SendResponse(HttpListenerResponse response, int statusCode, BaseResponse pipelineResponse)
        {
            response.StatusCode = 400;
            response.ContentType = "application/json";
            var json = JsonConvert.SerializeObject(pipelineResponse, Formatting.Indented);
            var buffer = Encoding.UTF8.GetBytes(json);
            response.ContentLength64 = buffer.Length;

            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }

        /// <summary>
        /// Send an /api/exec response and notify the transaction hook with the raw request and
        /// response JSON. Single send point for the exec handler so every branch (success and
        /// error) is captured uniformly.
        /// </summary>
        private async Task SendExecResponse(HttpListenerResponse response, int statusCode,
                                            BaseResponse body, string requestJson)
        {
            response.StatusCode = statusCode;
            response.ContentType = "application/json";
            var json = JsonConvert.SerializeObject(body, Formatting.Indented);
            var buffer = Encoding.UTF8.GetBytes(json);
            response.ContentLength64 = buffer.Length;

            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.OutputStream.Close();

            OnTransactionProcessed(requestJson, json);
        }

        /// <summary>
        /// Hook invoked after an /api/exec transaction is sent, with the raw request and response
        /// JSON. No-op by default (the Player never logs); the Editor server overrides this to
        /// write the transaction log.
        /// </summary>
        protected virtual void OnTransactionProcessed(string requestJson, string responseJson) { }

        /// <summary>
        /// Convert a single JSON parameter token to the command's parameter type.
        ///
        /// Agents and the CLI frequently pass structured parameters (e.g. <c>float[]</c> position,
        /// <c>JObject</c> properties) as a JSON-ENCODED STRING — e.g. position <c>"[0,0,0]"</c> or
        /// properties <c>"{\"m_Mass\":0.17}"</c>. Newtonsoft's <c>JValue(string).ToObject(float[])</c>
        /// (and likewise for <c>JObject</c>) returns null, so the parameter silently drops out:
        /// set_transform applies nothing and set_component_properties fails Required-parameter
        /// validation (CLI-219 / CLI-220). To fix this generally — without special-casing any command —
        /// when the token is a string but the target type is NOT string AND the trimmed string starts
        /// with '{' or '[', we re-parse it as a JSON document before converting.
        ///
        /// The '{'/'[' guard is deliberate and narrow: ordinary string params (and ObjectRef string
        /// handles like "/Player", "Assets/Foo.prefab", "guid:..." — none of which start with '{'/'[')
        /// fall straight through to <c>token.ToObject</c>, so the class-level
        /// <see cref="Unity.Pipeline.Models.ObjectRefConverter"/> and plain-string parameters keep
        /// working unchanged. A re-parse that fails falls through to the normal conversion path (the
        /// caller's try/catch then handles any remaining failure).
        /// </summary>
        private static object ConvertParameterToken(Newtonsoft.Json.Linq.JToken token, System.Type targetType)
        {
            if (token == null)
                return null;

            if (token.Type == Newtonsoft.Json.Linq.JTokenType.String
                && targetType != typeof(string)
                && !targetType.IsPrimitive
                && !targetType.IsEnum)
            {
                var s = token.Value<string>();
                if (!string.IsNullOrWhiteSpace(s))
                {
                    var t = s.TrimStart();
                    if (t.Length > 0 && (t[0] == '{' || t[0] == '['))
                    {
                        try { return Newtonsoft.Json.Linq.JToken.Parse(s).ToObject(targetType); }
                        catch (Newtonsoft.Json.JsonException) { /* fall through to the direct conversion */ }
                    }
                }
            }

            return token.ToObject(targetType);
        }

        /// <summary>
        /// Extract command parameters from JSON request and convert to appropriate types.
        /// </summary>
        private object[] ExtractCommandParameters(CommandInfo command, Newtonsoft.Json.Linq.JObject parametersJson)
        {
            var parameterValues = new object[command.Parameters.Count];

            for (int i = 0; i < command.Parameters.Count; i++)
            {
                var paramInfo = command.Parameters[i];
                var paramName = paramInfo.Name;

                // Try to get value from JSON parameters
                object jsonValue = null;
                if (parametersJson != null && parametersJson.ContainsKey(paramName))
                {
                    try
                    {
                        jsonValue = ConvertParameterToken(parametersJson[paramName], paramInfo.ParameterType);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Failed to convert parameter '{paramName}' to {paramInfo.ParameterType.Name}: {ex.Message}");
                        // Conversion failed, will use default value
                    }
                }

                // Use provided value or default value
                if (jsonValue != null)
                {
                    parameterValues[i] = jsonValue;
                }
                else if (paramInfo.DefaultValue != null)
                {
                    parameterValues[i] = paramInfo.DefaultValue;
                }
                else
                {
                    // Use type's default value for value types, null for reference types
                    parameterValues[i] = paramInfo.ParameterType.IsValueType
                        ? Activator.CreateInstance(paramInfo.ParameterType)
                        : null;
                }
            }

            return parameterValues;
        }

        /// <summary>
        /// Validate that all required command parameters are provided.
        /// </summary>
        private string ValidateCommandParameters(CommandInfo command, object[] parameters)
        {
            for (int i = 0; i < command.Parameters.Count; i++)
            {
                var paramInfo = command.Parameters[i];
                var value = parameters[i];

                if (paramInfo.Required && (value == null ||
                    (value is string str && string.IsNullOrEmpty(str))))
                {
                    return $"Required parameter '{paramInfo.Name}' is missing or empty";
                }
            }

            return null; // No validation errors
        }

        /// <summary>
        /// Execute a command by name with JSON parameters.
        /// Handles command lookup, parameter validation, and execution.
        /// Shared logic for both /api/exec and /api/editor_status endpoints.
        /// </summary>
        private async Task<object> ExecuteCommandByName(string commandName, JObject parametersJson)
        {
            // Find command
            var commands = CommandRegistry.DiscoverCommands().ToList();
            var command = commands.FirstOrDefault(c => c.Name == commandName);
            if (command == null)
            {
                var availableCommands = string.Join(", ", commands.Select(c => c.Name));
                var errorMessage = $"No command named '{commandName}' is available. Available: [{availableCommands}]";
                Debug.LogError($"ExecuteCommandByName: {errorMessage}");
                throw new InvalidOperationException(errorMessage);
            }

            // Extract parameters
            var parameters = ExtractCommandParameters(command, parametersJson);

            // Validate required parameters
            var validationError = ValidateCommandParameters(command, parameters);
            if (!string.IsNullOrEmpty(validationError))
            {
                Debug.LogError($"ExecuteCommandByName: Parameter validation failed: {validationError}");
                throw new ArgumentException(validationError);
            }

            // Execute command with appropriate threading
            return await ExecuteCommand(command, parameters);
        }

        /// <summary>
        /// Execute the command method with provided parameters.
        /// Handles main thread execution if required.
        /// </summary>
        private async Task<object> ExecuteCommand(CommandInfo command, object[] parameters)
        {
            object raw;
            if (command.MainThreadRequired)
            {
                // Execute on the main thread using THIS server's dispatcher.
                if (m_Dispatcher.IsMainThread())
                {
                    raw = ExecuteCommandDirect(command, parameters);
                }
                else
                {
                    raw = await Task.Run(() => m_Dispatcher.Invoke(() => ExecuteCommandDirect(command, parameters)));
                }
            }
            else
            {
                // Execute on background thread
                raw = await Task.Run(() => ExecuteCommandDirect(command, parameters));
            }

            return await UnwrapResult(raw);
        }

        /// <summary>
        /// Commands declared as `async Task`/`Task&lt;T&gt;` return their Task from reflection Invoke.
        /// Await it here (on the calling background thread, leaving the main thread free to pump the
        /// dispatcher) so the actual value is serialized rather than the Task itself, and so a faulted
        /// command surfaces its real exception to the request handler instead of being masked when
        /// Newtonsoft reads Task.Result during serialization.
        /// </summary>
        private static async Task<object> UnwrapResult(object result)
        {
            if (result is Task task)
            {
                await task.ConfigureAwait(false);
                // Task&lt;T&gt; exposes a Result property; non-generic Task does not.
                var resultProperty = task.GetType().GetProperty("Result");
                return resultProperty?.GetValue(task);
            }

            return result;
        }

        /// <summary>
        /// Direct command execution using reflection.
        /// </summary>
        private object ExecuteCommandDirect(CommandInfo command, object[] parameters)
        {
            try
            {
                return command.Method.Invoke(null, parameters);
            }
            catch (System.Reflection.TargetInvocationException tie) when (tie.InnerException != null)
            {
                // Reflection wraps the command's own exception in a TargetInvocationException; surface
                // the inner message so callers (and agents) get an actionable error instead of the
                // generic "Exception has been thrown by the target of an invocation."
                var inner = tie.InnerException;
                Debug.LogError($"Command '{command.Name}' failed: {inner.Message}");

                // Preserve validation-oriented exceptions so HandleExecRequest's dedicated
                // catch (ArgumentException) classifies them as "Parameter Validation Failed" rather than
                // collapsing them into "Command Execution Failed". Rethrow the original with its stack.
                if (inner is ArgumentException)
                    System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(inner).Throw();

                throw new InvalidOperationException($"Command '{command.Name}' failed: {inner.Message}", inner);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Command execution failed: {ex.Message}");
                throw new InvalidOperationException($"Command '{command.Name}' failed: {ex.Message}", ex);
            }
        }


        /// <summary>
        /// Get the port range for this server type.
        /// Editor servers use 7800-7899, Runtime servers use 7900-7999.
        /// </summary>
        protected virtual (int basePort, int maxPort) GetPortRange()
        {
            return (7800, 7849); // Editor production (test editor servers use 7850-7899)
        }

        /// <summary>
        /// Find an available port in the pipeline server range.
        /// </summary>
        private int FindAvailablePort()
        {
            var (basePort, maxPort) = GetPortRange();

            for (int port = basePort; port <= maxPort; port++)
            {
                if (IsPortAvailable(port))
                {
                    return port;
                }
            }

            throw new InvalidOperationException($"No available ports in range {basePort}-{maxPort}");
        }

        /// <summary>
        /// Check if a specific port is available for binding.
        /// </summary>
        private bool IsPortAvailable(int port)
        {
            try
            {
                using (var listener = new HttpListener())
                {
                    AddLoopbackPrefixes(listener, port);
                    listener.Start();
                    listener.Stop();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
