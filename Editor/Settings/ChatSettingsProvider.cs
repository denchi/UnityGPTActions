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
        private static bool _mcpAutoRetryEnabled;
        private static double _mcpRetryUntil;

        static ChatSettingsProvider()
        {
            EditorApplication.update += OnEditorUpdate;
        }

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
                    
                    // Common settings
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Common Configs", EditorStyles.boldLabel);
                    var commonPythonPath = NormalizeLibraryPyPath(EditorGUILayout.TextField("Python Path", DenormalizeLibraryPyPath(settings.McpPythonPath)));
                    var commonEnvPath = NormalizeLibraryPyPath(EditorGUILayout.TextField("Python Env Path", DenormalizeLibraryPyPath(settings.McpEnvPath)));
                    var commonPythonFallback = EditorGUILayout.TextField("Python Fallback", settings.McpPythonFallback);
                    if (settings.SearchApiPythonPath != commonPythonPath) settings.SearchApiPythonPath = commonPythonPath;
                    if (settings.McpPythonPath != commonPythonPath) settings.McpPythonPath = commonPythonPath;
                    if (settings.SearchApiEnvPath != commonEnvPath) settings.SearchApiEnvPath = commonEnvPath;
                    if (settings.McpEnvPath != commonEnvPath) settings.McpEnvPath = commonEnvPath;
                    if (settings.SearchApiPythonFallback != commonPythonFallback) settings.SearchApiPythonFallback = commonPythonFallback;
                    if (settings.McpPythonFallback != commonPythonFallback) settings.McpPythonFallback = commonPythonFallback;

                    if (GUILayout.Button("Wipe Python Environment"))
                    {
                        TryWipeEnvironment(settings.McpEnvPath, "Python");
                    }
                    
                    // Search API settings
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Search API Settings", EditorStyles.boldLabel);
                    settings.SearchApiHost = DrawStatusInlineTextField(
                        "Search API Host",
                        settings.SearchApiHost,
                        _searchAvailable,
                        _searchChecking,
                        _searchStatusKnown);
                    if (_lastSearchHost != settings.SearchApiHost)
                    {
                        _lastSearchHost = settings.SearchApiHost;
                        _searchStatusKnown = false;
                    }
                    
                    if (!_searchChecking && !_searchStatusKnown)
                    {
                        CheckSearchStatusAsync(settings);
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
                    settings.McpAutoStart = EditorGUILayout.Toggle("MCP Autostart", settings.McpAutoStart);
                    settings.McpUseUpdateQueue = EditorGUILayout.Toggle("MCP Use Update Queue", settings.McpUseUpdateQueue);
                    
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
                        // Trigger status refresh after start
                        _mcpStatusKnown = false;
                        _mcpAutoRetryEnabled = true;
                        _mcpRetryUntil = EditorApplication.timeSinceStartup + 10.0;
                    }                                    
                    
                    if (GUILayout.Button("Stop MCP Server"))
                    {
                        Mcp.McpServerController.StopAll();
                        // Immediately reflect offline status
                        _mcpBridgeAvailable = false;
                        _mcpServerAvailable = false;
                        _mcpBridgeChecking = false;
                        _mcpServerChecking = false;
                        _mcpStatusKnown = true;
                        _mcpAutoRetryEnabled = false;
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
                if (_mcpServerAvailable && _mcpBridgeAvailable)
                {
                    _mcpAutoRetryEnabled = false;
                }
            }
        }

        private static void OnEditorUpdate()
        {
            if (!_mcpAutoRetryEnabled)
                return;

            if (EditorApplication.timeSinceStartup > _mcpRetryUntil)
            {
                _mcpAutoRetryEnabled = false;
                return;
            }

            if (_mcpBridgeChecking || _mcpServerChecking)
                return;

            var settings = ChatSettings.instance;
            if (settings == null)
                return;

            CheckMcpStatusAsync(settings);
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

            // Always ensure dependencies are installed (fastapi, uvicorn, etc.).
            // This is needed even when the env exists but packages are missing.
            ExtractorRunner.TryCreatePythonEnvironment(
                settings.SearchApiPythonPath,
                settings.SearchApiEnvPath,
                settings.SearchApiPythonFallback);
        }

        private static void EnsureMcpEnvironment(ChatSettings settings)
        {
            if (settings == null)
                return;

            if (!IsPythonEnvMissing(settings.McpPythonPath, settings.McpEnvPath, "Library/py/mcp"))
                return;

            ExtractorRunner.TryCreateMcpEnvironment(
                settings.McpPythonPath,
                settings.McpEnvPath,
                settings.McpPythonFallback);
        }

        private static bool IsPythonEnvMissing(string pythonPath, string envPath, string defaultVenvName)
        {
            if (string.IsNullOrWhiteSpace(pythonPath))
                return true;

            if (!IsSimpleExecutableName(pythonPath))
            {
                var fullPath = Path.GetFullPath(pythonPath);
                return !File.Exists(fullPath);
            }

            var venvPath = Path.GetFullPath(string.IsNullOrWhiteSpace(envPath) ? defaultVenvName : envPath);
            return !Directory.Exists(venvPath);
        }

        private static bool IsSimpleExecutableName(string pythonPath)
        {
            return pythonPath.IndexOf(Path.DirectorySeparatorChar) == -1 &&
                   pythonPath.IndexOf(Path.AltDirectorySeparatorChar) == -1;
        }

        private static string NormalizeLibraryPyPath(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            var trimmed = input.Trim();

            if (Path.IsPathRooted(trimmed))
                return trimmed.Replace("\\", "/");

            if (IsSimpleExecutableName(trimmed))
                return trimmed;

            var normalized = trimmed.Replace("\\", "/");
            if (normalized.StartsWith("Library/py/", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "Library/py", StringComparison.OrdinalIgnoreCase))
            {
                return normalized;
            }

            return $"Library/py/{normalized}";
        }

        private static string DenormalizeLibraryPyPath(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            var normalized = input.Replace("\\", "/").Trim();
            if (normalized.Equals("Library/py", StringComparison.OrdinalIgnoreCase))
                return string.Empty;

            if (normalized.StartsWith("Library/py/", StringComparison.OrdinalIgnoreCase))
                return normalized.Substring("Library/py/".Length);

            return input;
        }

        private static void TryWipeEnvironment(string envPath, string label)
        {
            var targetPath = Path.GetFullPath(string.IsNullOrWhiteSpace(envPath) ? "" : envPath);
            if (string.IsNullOrWhiteSpace(envPath) || !Directory.Exists(targetPath))
            {
                EditorUtility.DisplayDialog(
                    "Wipe Environment",
                    $"{label} environment not found at '{targetPath}'.",
                    "OK");
                return;
            }

            var confirm = EditorUtility.DisplayDialog(
                "Wipe Environment",
                $"Delete '{targetPath}'? This cannot be undone.",
                "Delete",
                "Cancel");

            if (!confirm)
                return;

            try
            {
                Directory.Delete(targetPath, true);
                Debug.Log($"[ChatSettings] Wiped {label} environment at {targetPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[ChatSettings] Failed to wipe {label} environment: {e.Message}");
            }
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
