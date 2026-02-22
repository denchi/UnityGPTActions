using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GPTUnity.Settings;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Mcp
{
    internal static class HubAgentRegistrar
    {
        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };

        private static bool _enabled;
        private static bool _registering;
        private static bool _registered;
        private static bool _heartbeatInFlight;

        private static string _hubUrl;
        private static string _hubToken;
        private static string _sessionId;
        private static string _launchToken;
        private static string _agentToken;
        private static string _agentEndpoint;
        private static string _bridgeUrl;
        private static int _heartbeatSeconds;

        private static double _nextRegisterAt;
        private static double _nextHeartbeatAt;

        public static void Start(ChatSettings settings)
        {
            Stop();

            if (!TryLoadEnvironment(out _hubUrl, out _hubToken, out _sessionId, out _launchToken, out _heartbeatSeconds))
            {
                return;
            }

            _agentEndpoint = ResolveAgentEndpoint(McpServerController.ActiveMcpServerUrl);
            if (string.IsNullOrWhiteSpace(_agentEndpoint))
            {
                _agentEndpoint = ResolveAgentEndpoint(settings?.McpServerUrl);
            }

            _bridgeUrl = !string.IsNullOrWhiteSpace(McpServerController.ActiveBridgeUrl)
                ? McpServerController.ActiveBridgeUrl
                : settings?.McpBridgeUrlResolved ?? string.Empty;
            if (string.IsNullOrWhiteSpace(_agentEndpoint))
            {
                Debug.LogWarning("[MCP] Hub registrar skipped: could not resolve agent endpoint from MCP Server URL.");
                return;
            }

            _enabled = true;
            _nextRegisterAt = EditorApplication.timeSinceStartup + 0.5;
            EditorApplication.update += OnEditorUpdate;
            Debug.Log("[MCP] Hub registrar enabled for session " + _sessionId + " (mode=" + (McpServerController.IsHubMode ? "HUB" : "STANDALONE") + ")");
        }

        public static void Stop()
        {
            EditorApplication.update -= OnEditorUpdate;
            _enabled = false;
            _registering = false;
            _registered = false;
            _heartbeatInFlight = false;
            _agentToken = string.Empty;
            _hubUrl = string.Empty;
            _hubToken = string.Empty;
            _sessionId = string.Empty;
            _launchToken = string.Empty;
            _agentEndpoint = string.Empty;
            _bridgeUrl = string.Empty;
            _nextRegisterAt = 0;
            _nextHeartbeatAt = 0;
        }

        private static void OnEditorUpdate()
        {
            if (!_enabled)
            {
                return;
            }

            var now = EditorApplication.timeSinceStartup;
            if (!_registered)
            {
                if (!_registering && now >= _nextRegisterAt)
                {
                    _ = RegisterAgentAsync();
                }

                return;
            }

            if (!_heartbeatInFlight && now >= _nextHeartbeatAt)
            {
                _ = SendHeartbeatAsync();
            }
        }

        private static async Task RegisterAgentAsync()
        {
            if (!_enabled || _registering || _registered)
            {
                return;
            }

            _registering = true;
            try
            {
                if (!await IsAgentEndpointHealthyAsync())
                {
                    _nextRegisterAt = EditorApplication.timeSinceStartup + 2.0;
                    return;
                }

                var payload = new RegisterAgentRequest
                {
                    session_id = _sessionId,
                    launch_token = _launchToken,
                    endpoint = _agentEndpoint,
                    tool_manifest = new ToolManifest
                    {
                        source = "unity-python-mcp",
                        mode = McpServerController.IsHubMode ? "hub" : "standalone",
                        mcp_url = BuildUrl(_agentEndpoint, "/mcp"),
                        sse_url = BuildUrl(_agentEndpoint, "/mcp/sse"),
                        health_url = BuildUrl(_agentEndpoint, "/mcp/health"),
                        bridge_url = _bridgeUrl
                    }
                };

                var url = BuildUrl(_hubUrl, "/agents/register");
                using (var request = new HttpRequestMessage(HttpMethod.Post, url))
                {
                    request.Headers.TryAddWithoutValidation("X-Hub-Token", _hubToken);
                    request.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

                    using (var response = await Http.SendAsync(request))
                    {
                        var body = await response.Content.ReadAsStringAsync();
                        if (!response.IsSuccessStatusCode)
                        {
                            Debug.LogWarning($"[MCP] Hub register failed ({(int)response.StatusCode}): {body}");
                            _nextRegisterAt = EditorApplication.timeSinceStartup + 3.0;
                            return;
                        }

                        var model = JsonConvert.DeserializeObject<RegisterAgentResponse>(body);
                        if (model == null || !model.accepted || string.IsNullOrEmpty(model.agent_token))
                        {
                            Debug.LogWarning("[MCP] Hub register response missing agent token.");
                            _nextRegisterAt = EditorApplication.timeSinceStartup + 3.0;
                            return;
                        }

                        _agentToken = model.agent_token;
                        _registered = true;
                        _nextHeartbeatAt = EditorApplication.timeSinceStartup + _heartbeatSeconds;
                        Debug.Log("[MCP] Hub agent registered for session " + _sessionId);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[MCP] Hub register exception: " + ex.Message);
                _nextRegisterAt = EditorApplication.timeSinceStartup + 3.0;
            }
            finally
            {
                _registering = false;
            }
        }

        private static async Task SendHeartbeatAsync()
        {
            if (!_enabled || !_registered || string.IsNullOrEmpty(_agentToken))
            {
                return;
            }

            _heartbeatInFlight = true;
            try
            {
                var payload = new HeartbeatRequest
                {
                    session_id = _sessionId,
                    agent_token = _agentToken
                };

                var url = BuildUrl(_hubUrl, "/agents/heartbeat");
                using (var request = new HttpRequestMessage(HttpMethod.Post, url))
                {
                    request.Headers.TryAddWithoutValidation("X-Hub-Token", _hubToken);
                    request.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

                    using (var response = await Http.SendAsync(request))
                    {
                        if (!response.IsSuccessStatusCode)
                        {
                            var body = await response.Content.ReadAsStringAsync();
                            Debug.LogWarning($"[MCP] Hub heartbeat failed ({(int)response.StatusCode}): {body}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[MCP] Hub heartbeat exception: " + ex.Message);
            }
            finally
            {
                _heartbeatInFlight = false;
                _nextHeartbeatAt = EditorApplication.timeSinceStartup + _heartbeatSeconds;
            }
        }

        private static async Task<bool> IsAgentEndpointHealthyAsync()
        {
            try
            {
                var healthUrl = BuildUrl(_agentEndpoint, "/mcp/health");
                using (var response = await Http.GetAsync(healthUrl))
                {
                    return response.IsSuccessStatusCode;
                }
            }
            catch
            {
                return false;
            }
        }

        private static bool TryLoadEnvironment(
            out string hubUrl,
            out string hubToken,
            out string sessionId,
            out string launchToken,
            out int heartbeatSeconds)
        {
            hubUrl = (Environment.GetEnvironmentVariable("UNITY_MCP_HUB_URL") ?? string.Empty).TrimEnd('/');
            hubToken = Environment.GetEnvironmentVariable("UNITY_MCP_HUB_TOKEN") ?? string.Empty;
            sessionId = Environment.GetEnvironmentVariable("UNITY_MCP_SESSION_ID") ?? string.Empty;
            launchToken = Environment.GetEnvironmentVariable("UNITY_MCP_LAUNCH_TOKEN") ?? string.Empty;
            var heartbeatRaw = Environment.GetEnvironmentVariable("UNITY_MCP_HEARTBEAT_SECONDS") ?? string.Empty;

            if (!int.TryParse(heartbeatRaw, out heartbeatSeconds) || heartbeatSeconds < 3)
            {
                heartbeatSeconds = 10;
            }

            return !string.IsNullOrWhiteSpace(hubUrl)
                && !string.IsNullOrWhiteSpace(hubToken)
                && !string.IsNullOrWhiteSpace(sessionId)
                && !string.IsNullOrWhiteSpace(launchToken);
        }

        private static string ResolveAgentEndpoint(string mcpServerUrl)
        {
            if (string.IsNullOrWhiteSpace(mcpServerUrl))
            {
                return string.Empty;
            }

            if (!Uri.TryCreate(mcpServerUrl, UriKind.Absolute, out var uri))
            {
                return string.Empty;
            }

            var builder = new UriBuilder(uri.Scheme, uri.Host, uri.Port);
            return builder.Uri.ToString().TrimEnd('/');
        }

        private static string BuildUrl(string baseUrl, string path)
        {
            var b = (baseUrl ?? string.Empty).TrimEnd('/');
            var p = (path ?? string.Empty).TrimStart('/');
            return b + "/" + p;
        }

        private sealed class RegisterAgentRequest
        {
            public string session_id;
            public string launch_token;
            public string endpoint;
            public ToolManifest tool_manifest;
        }

        private sealed class ToolManifest
        {
            public string source;
            public string mode;
            public string mcp_url;
            public string sse_url;
            public string health_url;
            public string bridge_url;
        }

        private sealed class RegisterAgentResponse
        {
            public bool accepted;
            public string agent_token;
        }

        private sealed class HeartbeatRequest
        {
            public string session_id;
            public string agent_token;
        }
    }
}
