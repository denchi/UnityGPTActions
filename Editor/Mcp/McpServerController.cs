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

        static McpServerController()
        {
            EditorApplication.delayCall += TryAutoStart;
            EditorApplication.quitting += StopAll;
        }

        public static void StartAll(ChatSettings settings)
        {
            if (settings == null)
            {
                Debug.LogError("[MCP] ChatSettings missing.");
                return;
            }

            McpBridgeServer.Start(settings.McpBridgeUrl);
            StartPythonServer(settings);
            HubAgentRegistrar.Start(settings);
        }

        public static void StopAll()
        {
            HubAgentRegistrar.Stop();
            StopPythonServer(ChatSettings.instance);
            McpBridgeServer.Stop();
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

        private static void StartPythonServer(ChatSettings settings)
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

            if (!TryParseServerHost(settings.McpServerUrl, out var host, out var port))
            {
                Debug.LogError($"[MCP] Invalid MCP Server URL: {settings.McpServerUrl}");
                return;
            }

            if (!IsPortAvailable(host, port))
            {
                Debug.LogWarning($"[MCP] Port {port} is already in use. Maybe server is already running?");
                return;
            }

            var args = $"\"{scriptPath}\" --unity-url \"{settings.McpBridgeUrl}\" --host \"{host}\" --port {port}";
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

            Debug.Log($"[MCP] MCP server starting: {settings.McpPythonPathResolved} {args}");
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

            if (!TryParseServerHost(settings.McpServerUrl, out var host, out var port))
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
