using System;
using System.IO;
using System.Net;
using System.Net.Http;
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
        private static bool _lastSearchAutoHost;
        private static bool _mcpBridgeChecking;
        private static bool _mcpBridgeAvailable;
        private static bool _mcpServerChecking;
        private static bool _mcpServerAvailable;
        private static bool _mcpStatusKnown;
        private static string _lastMcpBridgeUrl;
        private static bool _lastMcpBridgeAutoUrl;
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
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField("Colors", EditorStyles.boldLabel);
                    var newColorBackgroundUser =
                        EditorGUILayout.ColorField("User Background Color", settings.ColorBackgroundUser);
                    var newColorBackgroundAssistant = EditorGUILayout.ColorField("Assistant Background Color",
                        settings.ColorBackgroundAssistant);
                    var newColorChatBackground =
                        EditorGUILayout.ColorField("Chat Background Color", settings.ColorChatBackground);
                    EditorGUILayout.EndVertical();

                    // Common settings
                    EditorGUILayout.Space();
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField("Common Configs", EditorStyles.boldLabel);
                    var commonPythonPath = ChatSettings.NormalizeLibraryPyRelative(EditorGUILayout.TextField("Python Path", settings.McpPythonPath));
                    var commonEnvPath = ChatSettings.NormalizeLibraryPyRelative(EditorGUILayout.TextField("Python Env Path", settings.McpEnvPath));
                    var commonPythonFallback = EditorGUILayout.TextField("Python Fallback", settings.McpPythonFallback);
                    if (settings.SearchApiPythonPath != commonPythonPath) settings.SearchApiPythonPath = commonPythonPath;
                    if (settings.McpPythonPath != commonPythonPath) settings.McpPythonPath = commonPythonPath;
                    if (settings.SearchApiEnvPath != commonEnvPath) settings.SearchApiEnvPath = commonEnvPath;
                    if (settings.McpEnvPath != commonEnvPath) settings.McpEnvPath = commonEnvPath;
                    if (settings.SearchApiPythonFallback != commonPythonFallback) settings.SearchApiPythonFallback = commonPythonFallback;
                    if (settings.McpPythonFallback != commonPythonFallback) settings.McpPythonFallback = commonPythonFallback;

                    if (GUILayout.Button("Wipe Python Environment"))
                    {
                        TryWipeEnvironment(settings, settings.McpEnvPath, "Python");
                    }
                    EditorGUILayout.EndVertical();
                    
                    // Search API settings
                    EditorGUILayout.Space();
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField("Search", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField("Indexing + semantic search service settings.", EditorStyles.miniLabel);
                    settings.SearchApiHost = DrawStatusInlineTextFieldWithAuto(
                        "Search API Host",
                        settings.SearchApiHost,
                        settings.SearchApiAutoHost,
                        out var searchAutoHost,
                        GetAutoDisplayUrl(settings.SearchApiHostResolved),
                        _searchAvailable,
                        _searchChecking,
                        _searchStatusKnown);
                    settings.SearchApiAutoHost = searchAutoHost;

                    if (_lastSearchHost != settings.SearchApiHost || _lastSearchAutoHost != settings.SearchApiAutoHost)
                    {
                        _lastSearchHost = settings.SearchApiHost;
                        _lastSearchAutoHost = settings.SearchApiAutoHost;
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

                    GUILayout.Space(4);

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

                    EditorGUILayout.EndVertical();

                    // MCP settings
                    EditorGUILayout.Space();
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField("MCP", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField("Bridge + MCP protocol server settings.", EditorStyles.miniLabel);
                    settings.McpBridgeUrl = DrawStatusInlineTextFieldWithAuto(
                        "MCP Bridge URL",
                        settings.McpBridgeUrl,
                        settings.McpBridgeAutoUrl,
                        out var mcpBridgeAutoUrl,
                        GetAutoDisplayUrl(settings.McpBridgeUrlResolved),
                        _mcpBridgeAvailable,
                        _mcpBridgeChecking,
                        _mcpStatusKnown);
                    settings.McpBridgeAutoUrl = mcpBridgeAutoUrl;
                    settings.McpServerUrl = DrawStatusInlineTextField(
                        "MCP Server URL",
                        settings.McpServerUrl,
                        _mcpServerAvailable,
                        _mcpServerChecking,
                        _mcpStatusKnown);
                    EditorGUILayout.LabelField("MCP Runtime Mode", Mcp.McpServerController.IsHubMode ? "HUB" : "STANDALONE");
                    settings.McpAutoStart = EditorGUILayout.Toggle("MCP Autostart", settings.McpAutoStart);
                    settings.McpUseUpdateQueue = EditorGUILayout.Toggle("MCP Use Update Queue", settings.McpUseUpdateQueue);

                    if (_lastMcpBridgeUrl != settings.McpBridgeUrl ||
                        _lastMcpBridgeAutoUrl != settings.McpBridgeAutoUrl ||
                        _lastMcpServerUrl != settings.McpServerUrl)
                    {
                        _lastMcpBridgeUrl = settings.McpBridgeUrl;
                        _lastMcpBridgeAutoUrl = settings.McpBridgeAutoUrl;
                        _lastMcpServerUrl = settings.McpServerUrl;
                        _mcpStatusKnown = false;
                    }

                    if (!_mcpStatusKnown && !_mcpBridgeChecking && !_mcpServerChecking)
                    {
                        CheckMcpStatusAsync(settings);
                    }

                    GUILayout.Space(4);

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
                    EditorGUILayout.EndVertical();

                    if (EditorGUI.EndChangeCheck())
                    {
                        settings.ColorBackgroundUser = newColorBackgroundUser;
                        settings.ColorBackgroundAssistant = newColorBackgroundAssistant;
                        settings.ColorChatBackground = newColorChatBackground;
                        settings.SaveSettings(true);
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
                var client = new DeepSearchClient(settings.SearchApiHostResolved, settings.SearchApiPythonPathResolved);
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
                    var response = await client.GetAsync(settings.SearchApiHostResolved.TrimEnd('/') + "/ping");
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
                    var bridgeUrl = settings.McpBridgeUrlResolved.TrimEnd('/') + "/health";
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

        private static string DrawStatusInlineTextFieldWithAuto(
            string label,
            string configuredValue,
            bool autoEnabled,
            out bool newAutoEnabled,
            string autoDisplayValue,
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
            using (new EditorGUI.DisabledScope(autoEnabled))
            {
                var shownValue = autoEnabled ? (autoDisplayValue ?? string.Empty) : configuredValue;
                var editedValue = EditorGUILayout.TextField(label, shownValue);
                if (!autoEnabled)
                {
                    configuredValue = editedValue;
                }
            }
            newAutoEnabled = GUILayout.Toggle(autoEnabled, "Auto", GUILayout.Width(54));
            EditorGUILayout.LabelField(statusText, style, GUILayout.Width(90));
            EditorGUILayout.EndHorizontal();

            return configuredValue;
        }

        private static string GetAutoDisplayUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return string.Empty;

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return string.Empty;

            if (!IPAddress.TryParse(uri.Host, out _))
                return string.Empty;

            return $"{uri.Scheme}://{uri.Host}:{uri.Port}";
        }

        private static bool EnsureSearchEnvironment(ChatSettings settings)
        {
            if (settings == null)
                return false;

            // Always ensure dependencies are installed (fastapi, uvicorn, etc.).
            // This is needed even when the env exists but packages are missing.
            return ExtractorRunner.TryCreatePythonEnvironment(
                settings.SearchApiPythonPathResolved,
                settings.SearchApiEnvPathResolved,
                settings.SearchApiPythonFallback);
        }

        private static bool EnsureMcpEnvironment(ChatSettings settings)
        {
            if (settings == null)
                return false;

            if (!IsPythonEnvMissing(settings.McpPythonPathResolved, settings.McpEnvPathResolved, ChatSettings.ResolveLibraryPyPath("venv_mcp")))
                return false;

            return ExtractorRunner.TryCreateMcpEnvironment(
                settings.McpPythonPathResolved,
                settings.McpEnvPathResolved,
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

        private static void TryWipeEnvironment(ChatSettings settings, string envPath, string label)
        {
            var resolvedPath = ChatSettings.ResolveLibraryPyPath(envPath);
            var targetPath = Path.GetFullPath(string.IsNullOrWhiteSpace(resolvedPath) ? "" : resolvedPath);
            if (string.IsNullOrWhiteSpace(resolvedPath) || !Directory.Exists(targetPath))
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
