using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using DeathByGravity.GPTActions;
using GPTUnity.Indexing;
using GPTUnity.Settings;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace GPTUnity.Data
{
    public class ChatSettingsProvider
    {
        private static readonly string PreferencesPath = "Project/Chat Settings";
        private static bool _searchChecking;
        private static bool _searchAvailable;
        private static bool _searchStatusKnown;
        private static string _lastSearchHost;
        private static bool _mcpBridgeChecking;
        private static bool _mcpBridgeAvailable;
        private static bool _mcpServerChecking;
        private static bool _mcpServerAvailable;
        private static bool _mcpStatusKnown;
        private static string _lastMcpBridgeUrl;
        private static string _lastMcpServerUrl;

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new SettingsProvider(PreferencesPath, SettingsScope.Project)
            {
                label = "Chat Settings",
                guiHandler = (searchContext) =>
                {
                    var settings = ChatSettings.instance;
                    EditorGUI.BeginChangeCheck();

                    // Color fields
                    var newColorBackgroundUser =
                        EditorGUILayout.ColorField("User Background Color", settings.ColorBackgroundUser);
                    var newColorBackgroundAssistant = EditorGUILayout.ColorField("Assistant Background Color",
                        settings.ColorBackgroundAssistant);
                    var newColorChatBackground =
                        EditorGUILayout.ColorField("Chat Background Color", settings.ColorChatBackground);
                    
                    // Search API settings
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Search API Settings", EditorStyles.boldLabel);
                    settings.SearchApiHost = EditorGUILayout.TextField("Search API Host", settings.SearchApiHost);
                    settings.SearchApiPythonPath = EditorGUILayout.TextField("Search API Python Path", settings.SearchApiPythonPath);

                    if (_lastSearchHost != settings.SearchApiHost)
                    {
                        _lastSearchHost = settings.SearchApiHost;
                        _searchStatusKnown = false;
                    }
                    
                    DrawStatusRow("Search API Status", _searchAvailable, _searchChecking, _searchStatusKnown);
                    if (!_searchChecking && !_searchStatusKnown)
                    {
                        CheckSearchStatusAsync(settings);
                    }
                    
                    if (GUILayout.Button("Test Search API Connection"))
                    {
                        // Run the check asynchronously to avoid blocking the main thread
                        CheckSearchApiAvailableAsync(settings);
                    }
                    
                    if (GUILayout.Button("Create Python Environment"))
                    {
                        ExtractorRunner.TryCreatePythonEnvironment(settings.SearchApiPythonPath);
                    }
                    
                    if (GUILayout.Button("Create a new index"))
                    {
                        ExtractorRunner.RunExtractor(settings);
                        ExtractorRunner.RunIndexer(settings);
                    }
                    
                    GUILayout.Space(10);
                    
                    if (GUILayout.Button("Start Server"))
                    {
                        // Run the check asynchronously to avoid blocking the main thread
                        StartServerAsync(settings);
                    }
                    
                    if (GUILayout.Button("Stop Server"))
                    {
                        // Run the check asynchronously to avoid blocking the main thread
                        StopServerAsync(settings);
                    }
                    
                    // MCP settings
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("MCP Settings", EditorStyles.boldLabel);
                    settings.McpBridgeUrl = DrawStatusInlineTextField(
                        "MCP Bridge URL",
                        settings.McpBridgeUrl,
                        _mcpBridgeAvailable,
                        _mcpBridgeChecking,
                        _mcpStatusKnown);
                    settings.McpServerUrl = DrawStatusInlineTextField(
                        "MCP Server URL",
                        settings.McpServerUrl,
                        _mcpServerAvailable,
                        _mcpServerChecking,
                        _mcpStatusKnown);
                    settings.McpPythonPath = EditorGUILayout.TextField("MCP Python Path", settings.McpPythonPath);
                    settings.McpAutoStart = EditorGUILayout.Toggle("MCP Autostart", settings.McpAutoStart);
                    
                    if (_lastMcpBridgeUrl != settings.McpBridgeUrl || _lastMcpServerUrl != settings.McpServerUrl)
                    {
                        _lastMcpBridgeUrl = settings.McpBridgeUrl;
                        _lastMcpServerUrl = settings.McpServerUrl;
                        _mcpStatusKnown = false;
                    }

                    if (!_mcpStatusKnown && !_mcpBridgeChecking && !_mcpServerChecking)
                    {
                        CheckMcpStatusAsync(settings);
                    }

                    GUILayout.Space(10);

                    if (GUILayout.Button("Start MCP Server"))
                    {
                        EnsureMcpEnvironment(settings);
                        Mcp.McpServerController.StartAll(settings);
                    }                                    
                    
                    if (GUILayout.Button("Stop MCP Server"))
                    {
                        Mcp.McpServerController.StopAll();
                    }
                    
                    if (GUILayout.Button("Refresh MCP Status"))
                    {
                        CheckMcpStatusAsync(settings);
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        settings.ColorBackgroundUser = newColorBackgroundUser;
                        settings.ColorBackgroundAssistant = newColorBackgroundAssistant;
                        settings.ColorChatBackground = newColorChatBackground;
                    }
                }
            };
        }

        private static async void CheckSearchApiAvailableAsync(ChatSettings settings)
        {
            bool isAvailable = false;
            
            var window = EditorWindow.GetWindow<ChatEditorWindow>();
            if (window != null && window.SearchApiClient != null)
            {
                var client = window.SearchApiClient;
                try
                {
                    isAvailable = await client.IsServerAvailable();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error checking Search API availability: {e.Message}");
                    isAvailable = false;
                }
            }
            else
            {
                // If the window or client is not available, create a new client
                var client = new DeepSearchClient(settings.SearchApiHost, settings.SearchApiPythonPath);
                try
                {
                    isAvailable = await client.IsServerAvailable();
                }
                catch
                {
                    isAvailable = false;
                }
            }
            
            EditorUtility.DisplayDialog("Search API Test", isAvailable ? "Connection successful!" : "Connection failed!", "OK");
        }

        private static async void CheckSearchStatusAsync(ChatSettings settings)
        {
            if (_searchChecking)
                return;

            _searchChecking = true;
            _searchStatusKnown = false;
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(2);
                    var response = await client.GetAsync(settings.SearchApiHost.TrimEnd('/') + "/ping");
                    _searchAvailable = response.IsSuccessStatusCode;
                }
            }
            catch
            {
                _searchAvailable = false;
            }
            finally
            {
                _searchChecking = false;
                _searchStatusKnown = true;
            }
        }

        private static async void CheckMcpStatusAsync(ChatSettings settings)
        {
            if (_mcpBridgeChecking || _mcpServerChecking)
                return;

            _mcpBridgeChecking = true;
            _mcpServerChecking = true;
            _mcpStatusKnown = false;

            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(2);
                    var bridgeUrl = settings.McpBridgeUrl.TrimEnd('/') + "/health";
                    var bridgeResponse = await client.GetAsync(bridgeUrl);
                    _mcpBridgeAvailable = bridgeResponse.IsSuccessStatusCode;
                }
            }
            catch
            {
                _mcpBridgeAvailable = false;
            }
            finally
            {
                _mcpBridgeChecking = false;
            }

            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(2);
                    if (Uri.TryCreate(settings.McpServerUrl, UriKind.Absolute, out var uri))
                    {
                        var serverHealthUrl = $"{uri.Scheme}://{uri.Host}:{uri.Port}/mcp/health";
                        var serverResponse = await client.GetAsync(serverHealthUrl);
                        _mcpServerAvailable = serverResponse.IsSuccessStatusCode;
                    }
                    else
                    {
                        _mcpServerAvailable = false;
                    }
                }
            }
            catch
            {
                _mcpServerAvailable = false;
            }
            finally
            {
                _mcpServerChecking = false;
                _mcpStatusKnown = true;
            }
        }

        private static void DrawStatusRow(string label, bool isOnline, bool isChecking, bool statusKnown)
        {
            var statusText = isChecking
                ? "Checking..."
                : statusKnown
                    ? (isOnline ? "Online" : "Offline")
                    : "Unknown";
            
            var color = isChecking
                ? new Color(1f, 0.75f, 0.2f)
                : statusKnown
                    ? (isOnline ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.9f, 0.2f, 0.2f))
                    : new Color(0.7f, 0.7f, 0.7f);

            var style = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = color }
            };

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(160));
            EditorGUILayout.LabelField(statusText, style);
            EditorGUILayout.EndHorizontal();
        }

        private static string DrawStatusInlineTextField(
            string label,
            string value,
            bool isOnline,
            bool isChecking,
            bool statusKnown)
        {
            var statusText = isChecking
                ? "Checking..."
                : statusKnown
                    ? (isOnline ? "Online" : "Offline")
                    : "Unknown";

            var color = isChecking
                ? new Color(1f, 0.75f, 0.2f)
                : statusKnown
                    ? (isOnline ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.9f, 0.2f, 0.2f))
                    : new Color(0.7f, 0.7f, 0.7f);

            var style = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = color }
            };

            EditorGUILayout.BeginHorizontal();
            value = EditorGUILayout.TextField(label, value);
            EditorGUILayout.LabelField(statusText, style, GUILayout.Width(90));
            EditorGUILayout.EndHorizontal();

            return value;
        }

        private static void EnsureSearchEnvironment(ChatSettings settings)
        {
            if (settings == null)
                return;

            if (!IsPythonEnvMissing(settings.SearchApiPythonPath, "venv"))
                return;

            ExtractorRunner.TryCreatePythonEnvironment(settings.SearchApiPythonPath);
        }

        private static void EnsureMcpEnvironment(ChatSettings settings)
        {
            if (settings == null)
                return;

            if (!IsPythonEnvMissing(settings.McpPythonPath, "venv_mcp"))
                return;

            ExtractorRunner.TryCreateMcpEnvironment(settings.McpPythonPath);
        }

        private static bool IsPythonEnvMissing(string pythonPath, string defaultVenvName)
        {
            if (string.IsNullOrWhiteSpace(pythonPath))
                return true;

            if (!IsSimpleExecutableName(pythonPath))
            {
                var fullPath = Path.GetFullPath(pythonPath);
                return !File.Exists(fullPath);
            }

            var venvPath = Path.GetFullPath(defaultVenvName);
            return !Directory.Exists(venvPath);
        }

        private static bool IsSimpleExecutableName(string pythonPath)
        {
            return pythonPath.IndexOf(Path.DirectorySeparatorChar) == -1 &&
                   pythonPath.IndexOf(Path.AltDirectorySeparatorChar) == -1;
        }
        
        private static async void StartServerAsync(ChatSettings settings)
        {
            EnsureSearchEnvironment(settings);
            bool isAvailable = false;
            
            var window = EditorWindow.GetWindow<ChatEditorWindow>();
            if (window != null && window.SearchApiClient != null)
            {
                var client = window.SearchApiClient;
                try
                {
                    isAvailable = await client.IsServerAvailable();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error checking Search API availability: {e.Message}");
                    isAvailable = false;
                }

                if (!isAvailable)
                {
                    await client.StartSearchServerAsync();
                    Debug.Log("[ChatSettingsProvider] Search server started successfully.");
                    return;
                }
                
                Debug.Log("[ChatSettingsProvider] Search server is already running.");
            }
        }
        
        private static async void StopServerAsync(ChatSettings settings)
        {
            var window = EditorWindow.GetWindow<ChatEditorWindow>();
            if (window != null && window.SearchApiClient != null)
            {
                var client = window.SearchApiClient;
                try
                {
                    await client.StopSearchServer();
                    Debug.Log("[ChatSettingsProvider] Search server stopped successfully.");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error stopping Search API server: {e.Message}");
                }
            }
        }
    }
}
