using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using GPTUnity.Settings;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Mcp
{
    [InitializeOnLoad]
    public static class McpServerController
    {
        private static Process _pythonProcess;
        public static bool IsHubMode { get; private set; }
        public static string ActiveBridgeUrl { get; private set; } = string.Empty;
        public static string ActiveMcpServerUrl { get; private set; } = string.Empty;

        static McpServerController()
        {
            EditorApplication.delayCall += TryAutoStart;
            EditorApplication.quitting += StopAll;
        }

        public static bool StartAll(ChatSettings settings)
        {
            if (settings == null)
            {
                Debug.LogError("[MCP] ChatSettings missing.");
                return false;
            }

            if (!TryResolveRuntimeUrls(settings, out var bridgeUrl, out var serverUrl, out var isHubMode))
            {
                return false;
            }

            IsHubMode = isHubMode;
            ActiveBridgeUrl = bridgeUrl;
            ActiveMcpServerUrl = serverUrl;

            Debug.Log($"[MCP] Runtime mode: {(IsHubMode ? "HUB" : "STANDALONE")}. bridge={ActiveBridgeUrl} server={ActiveMcpServerUrl}");

            McpBridgeServer.Start(ActiveBridgeUrl);
            StartPythonServer(settings, ActiveBridgeUrl, ActiveMcpServerUrl);
            HubAgentRegistrar.Start(settings);
            return true;
        }

        public static void StopAll()
        {
            HubAgentRegistrar.Stop();
            StopPythonServer(ChatSettings.instance);
            McpBridgeServer.Stop();
            ActiveBridgeUrl = string.Empty;
            ActiveMcpServerUrl = string.Empty;
            IsHubMode = false;
        }

        private static void TryAutoStart()
        {
            var settings = ChatSettings.instance;
            if (settings == null || !settings.McpAutoStart)
                return;

            if (!IsMcpEnvironmentReady(settings))
            {
                Debug.Log("[MCP] Autostart skipped: MCP environment is not set up.");
                return;
            }

            StartAll(settings);
        }

        private static void StartPythonServer(ChatSettings settings, string runtimeBridgeUrl, string runtimeServerUrl)
        {
            if (_pythonProcess != null && !_pythonProcess.HasExited)
            {
                Debug.Log("[MCP] MCP server is already running.");
                return;
            }

            var scriptPath = Path.GetFullPath(
                "Packages/com.deathbygravitystudio.gptactions/Editor/Mcp/mcp_server.py"
            );

            if (!File.Exists(scriptPath))
            {
                Debug.LogError($"[MCP] MCP server script not found at: {scriptPath}");
                return;
            }

            if (string.IsNullOrWhiteSpace(settings.McpPythonPathResolved))
            {
                Debug.LogError("[MCP] MCP Python path is not set.");
                return;
            }

            var pythonExe = ResolvePythonExecutable(settings.McpPythonPathResolved);
            if (!IsSimpleExecutableName(pythonExe) && !File.Exists(pythonExe))
            {
                Debug.LogWarning($"[MCP] MCP Python path not found: {pythonExe}. Falling back to 'python3'.");
                pythonExe = "python3";
            }

            if (!TryParseServerHost(runtimeServerUrl, out var host, out var port))
            {
                Debug.LogError($"[MCP] Invalid MCP Server URL: {runtimeServerUrl}");
                return;
            }

            if (!IsPortAvailable(host, port))
            {
                Debug.LogWarning($"[MCP] Port {port} is already in use. Maybe server is already running?");
                return;
            }

            var args = $"\"{scriptPath}\" --unity-url \"{runtimeBridgeUrl}\" --host \"{host}\" --port {port}";
            var startInfo = new ProcessStartInfo(pythonExe, args)
            {
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            startInfo.Environment["MCP_LOG_PAYLOAD"] = "1";
            startInfo.Environment["MCP_LOG_LEVEL"] = "debug";

            _pythonProcess = new Process();
            _pythonProcess.StartInfo = startInfo;
            _pythonProcess.OutputDataReceived += LogOutput;
            _pythonProcess.ErrorDataReceived += LogError;

            _pythonProcess.Start();
            _pythonProcess.BeginOutputReadLine();
            _pythonProcess.BeginErrorReadLine();

            Debug.Log($"[MCP] MCP server starting: {pythonExe} {args}");
        }

        private static bool TryResolveRuntimeUrls(
            ChatSettings settings,
            out string bridgeUrl,
            out string serverUrl,
            out bool isHubMode)
        {
            bridgeUrl = settings.McpBridgeUrlResolved;
            serverUrl = settings.McpServerUrl;
            var modeRaw = (Environment.GetEnvironmentVariable("UNITY_MCP_MODE") ?? string.Empty).Trim();
            var hasHubSession = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("UNITY_MCP_SESSION_ID"));
            var hasHubToken = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("UNITY_MCP_HUB_TOKEN"));
            var hubServerUrl = (Environment.GetEnvironmentVariable("UNITY_MCP_SERVER_URL") ?? string.Empty).Trim();
            var explicitHubMode = string.Equals(modeRaw, "hub", StringComparison.OrdinalIgnoreCase);

            isHubMode = explicitHubMode || (hasHubSession && hasHubToken && !string.IsNullOrWhiteSpace(hubServerUrl));

            if (isHubMode)
            {
                if (string.IsNullOrWhiteSpace(hubServerUrl))
                {
                    Debug.LogWarning("[MCP] Hub mode requested but UNITY_MCP_SERVER_URL is missing. Falling back to standalone mode.");
                    isHubMode = false;
                }
                else
                {
                    serverUrl = EnsureMcpPath(hubServerUrl);

                    var hubBridgeUrl = (Environment.GetEnvironmentVariable("UNITY_MCP_BRIDGE_URL") ?? string.Empty).Trim();
                    if (!string.IsNullOrWhiteSpace(hubBridgeUrl))
                    {
                        bridgeUrl = hubBridgeUrl.TrimEnd('/');
                    }
                }
            }

            serverUrl = EnsureMcpPath(serverUrl);

            if (string.IsNullOrWhiteSpace(bridgeUrl))
            {
                Debug.LogError("[MCP] Resolved bridge URL is empty.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(serverUrl))
            {
                Debug.LogError("[MCP] Resolved server URL is empty.");
                return false;
            }

            return true;
        }

        private static string EnsureMcpPath(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return url;

            var path = (uri.AbsolutePath ?? string.Empty).TrimEnd('/');
            if (path.EndsWith("/mcp", StringComparison.OrdinalIgnoreCase))
                return uri.GetLeftPart(UriPartial.Path).TrimEnd('/');

            var builder = new UriBuilder(uri)
            {
                Path = "/mcp"
            };

            return builder.Uri.ToString().TrimEnd('/');
        }

        private static void StopPythonServer(ChatSettings settings)
        {
            HubAgentRegistrar.Stop();
            TryRequestShutdown(settings);

            if (_pythonProcess == null || _pythonProcess.HasExited)
            {
                return;
            }

            try
            {
                _pythonProcess.Kill();
                _pythonProcess.WaitForExit(2000);
            }
            catch (Exception e)
            {
                Debug.LogError($"[MCP] Failed to stop MCP server: {e.Message}");
            }
            finally
            {
                try
                {
                    _pythonProcess?.Dispose();
                }
                catch
                {
                    // ignore dispose errors
                }

                _pythonProcess = null;
                Debug.Log("[MCP] MCP server stopped.");
            }
        }

        private static bool TryParseServerHost(string url, out string host, out int port)
        {
            host = "127.0.0.1";
            port = 7072;

            if (string.IsNullOrWhiteSpace(url))
                return false;

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return false;

            host = uri.Host;
            port = uri.Port;
            return true;
        }

        private static bool IsPortAvailable(string host, int port)
        {
            try
            {
                var address = IPAddress.TryParse(host, out var parsed)
                    ? parsed
                    : IPAddress.Loopback;
                var listener = new TcpListener(address, port);
                listener.Start();
                listener.Stop();
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
            catch
            {
                return false;
            }
        }

        private static void TryRequestShutdown(ChatSettings settings)
        {
            if (settings == null)
                return;

            var activeServerUrl = !string.IsNullOrWhiteSpace(ActiveMcpServerUrl)
                ? ActiveMcpServerUrl
                : settings.McpServerUrl;

            if (!TryParseServerHost(activeServerUrl, out var host, out var port))
                return;

            var shutdownUrl = $"http://{host}:{port}/mcp/shutdown";
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(1);
                    var response = client.PostAsync(shutdownUrl, new StringContent("")).GetAwaiter().GetResult();
                    if (!response.IsSuccessStatusCode)
                    {
                        Debug.LogWarning($"[MCP] Shutdown request failed: {response.StatusCode}");
                    }
                }
            }
            catch
            {
                // ignore shutdown errors; fallback to process kill
            }
        }

        private static string ResolvePythonExecutable(string pythonPath)
        {
            if (IsSimpleExecutableName(pythonPath))
                return pythonPath;

            return Path.GetFullPath(pythonPath);
        }

        private static bool IsSimpleExecutableName(string pythonPath)
        {
            return pythonPath.IndexOf(Path.DirectorySeparatorChar) == -1 &&
                   pythonPath.IndexOf(Path.AltDirectorySeparatorChar) == -1;
        }

        private static bool IsMcpEnvironmentReady(ChatSettings settings)
        {
            if (settings == null)
                return false;

            if (string.IsNullOrWhiteSpace(settings.McpEnvPathResolved))
                return false;

            var envPath = Path.GetFullPath(settings.McpEnvPathResolved);
            if (!Directory.Exists(envPath))
                return false;

            var pythonPath = settings.McpPythonPathResolved;
            if (string.IsNullOrWhiteSpace(pythonPath))
                return false;

            if (IsSimpleExecutableName(pythonPath))
                return true;

            return File.Exists(Path.GetFullPath(pythonPath));
        }

        private static void LogOutput(object sender, DataReceivedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Data))
                return;
            Debug.Log("[MCP] " + e.Data);
        }

        private static void LogError(object sender, DataReceivedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Data))
                return;
            if (e.Data.StartsWith("INFO:", StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log("[MCP] " + e.Data);
                return;
            }
            Debug.LogError("[MCP] " + e.Data);
        }
    }
}
