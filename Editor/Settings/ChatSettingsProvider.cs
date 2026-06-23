using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Linq;
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
        private static bool _searchAutoRetryEnabled;
        private static double _searchRetryUntil;
        private static bool _searchRebuildInProgress;
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
        private static bool _mcpToolsExpanded = true;
        private static bool _mcpLogsExpanded;
        private static Vector2 _mcpToolsScroll;
        private static Vector2 _mcpLogsScroll;

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

                    DrawSearchStatusSummary();
                    GUILayout.Space(4);
                    DrawSearchControls(settings);
                    GUILayout.Space(8);
                    DrawSearchIndexControls(settings);

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
                    settings.McpDebugLogging = EditorGUILayout.Toggle(
                        new GUIContent("MCP Debug Logging", "Capture verbose MCP bridge/server logs for debugging."),
                        settings.McpDebugLogging);

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

                    DrawMcpStatusSummary();
                    GUILayout.Space(4);
                    DrawMcpControls(settings);
                    GUILayout.Space(8);
                    DrawMcpTools(settings);
                    GUILayout.Space(8);
                    DrawMcpLogs(settings);
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

            var client = GetSearchApiClient(settings);
            if (client != null)
            {
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
            var settings = ChatSettings.instance;
            if (settings == null)
                return;

            if (_searchAutoRetryEnabled)
            {
                if (EditorApplication.timeSinceStartup > _searchRetryUntil)
                {
                    _searchAutoRetryEnabled = false;
                }
                else if (!_searchChecking)
                {
                    CheckSearchStatusAsync(settings);
                }
            }

            if (_mcpAutoRetryEnabled)
            {
                if (EditorApplication.timeSinceStartup > _mcpRetryUntil)
                {
                    _mcpAutoRetryEnabled = false;
                }
                else if (!_mcpBridgeChecking && !_mcpServerChecking)
                {
                    CheckMcpStatusAsync(settings);
                }
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

        private static void DrawMcpStatusSummary()
        {
            var message = BuildMcpStatusSummary();
            var messageType = (!_mcpStatusKnown || _mcpBridgeChecking || _mcpServerChecking)
                ? MessageType.Info
                : (_mcpBridgeAvailable && _mcpServerAvailable ? MessageType.Info : MessageType.Warning);
            EditorGUILayout.HelpBox(message, messageType);
        }

        private static void DrawSearchStatusSummary()
        {
            var message = BuildSearchStatusSummary();
            var messageType = !_searchStatusKnown || _searchChecking
                ? MessageType.Info
                : (_searchAvailable ? MessageType.Info : MessageType.Warning);
            EditorGUILayout.HelpBox(message, messageType);
        }

        private static string BuildSearchStatusSummary()
        {
            if (_searchChecking)
                return "Checking search service status...";

            if (!_searchStatusKnown)
                return "Search service status has not been checked yet.";

            if (_searchAvailable)
                return "Search is ready. The local semantic search server is online.";

            return "Search is offline. Start the local search service to enable semantic indexing and queries.";
        }

        private static void DrawSearchControls(ChatSettings settings)
        {
            var searchExpectedRunning = IsSearchExpectedRunning();
            var canStart = !searchExpectedRunning;
            var canStop = searchExpectedRunning;

            EditorGUILayout.BeginHorizontal();

            using (new EditorGUI.DisabledScope(!canStart))
            {
                if (GUILayout.Button(new GUIContent("Start Search", "Start or reconnect the local semantic search server.")))
                {
                    StartServerAsync(settings);
                }
            }

            using (new EditorGUI.DisabledScope(!canStop))
            {
                if (GUILayout.Button(new GUIContent("Stop Search", "Stop the local semantic search server.")))
                {
                    StopServerAsync(settings);
                }
            }

            if (GUILayout.Button(new GUIContent("Check Status", "Refresh the local search server health check.")))
            {
                CheckSearchStatusAsync(settings);
            }

            EditorGUILayout.EndHorizontal();
        }

        private static void DrawSearchIndexControls(ChatSettings settings)
        {
            EditorGUILayout.LabelField("Search Index", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Rebuild the extracted project data and refresh the semantic index used by search.", EditorStyles.miniLabel);

            using (new EditorGUI.DisabledScope(_searchRebuildInProgress))
            {
                if (GUILayout.Button(new GUIContent("Rebuild Search Index", "Re-extract project data and rebuild the semantic search index.")))
                {
                    RebuildSearchIndexAsync(settings);
                }
            }

            if (_searchRebuildInProgress)
            {
                EditorGUILayout.HelpBox("Rebuilding the search index. Check the Unity Console for extractor and indexer progress.", MessageType.Info);
            }
        }

        private static string BuildMcpStatusSummary()
        {
            if (_mcpBridgeChecking || _mcpServerChecking)
                return "Checking MCP service status...";

            if (!_mcpStatusKnown)
                return "MCP status has not been checked yet.";

            if (_mcpBridgeAvailable && _mcpServerAvailable)
                return "MCP is ready. The Unity bridge and Python MCP server are both online.";

            if (_mcpBridgeAvailable && !_mcpServerAvailable)
                return "The Unity bridge is online, but the Python MCP server is offline.";

            if (!_mcpBridgeAvailable && _mcpServerAvailable)
                return "The Python MCP server is online, but the Unity bridge is offline.";

            return "MCP is offline. Start the services to make the tools discoverable.";
        }

        private static void DrawMcpControls(ChatSettings settings)
        {
            var mcpExpectedRunning = IsMcpExpectedRunning();
            var canStart = !mcpExpectedRunning;
            var canStop = mcpExpectedRunning;

            EditorGUILayout.BeginHorizontal();

            using (new EditorGUI.DisabledScope(!canStart))
            {
                if (GUILayout.Button(new GUIContent("Start MCP", "Start or reconnect the Unity bridge and Python MCP server.")))
                {
                    if (EnsureMcpEnvironment(settings))
                    {
                        Mcp.McpServerController.StartAll(settings);
                        _mcpStatusKnown = false;
                        _mcpAutoRetryEnabled = true;
                        _mcpRetryUntil = EditorApplication.timeSinceStartup + 10.0;
                    }
                    else
                    {
                        Debug.LogError("[ChatSettingsProvider] MCP environment setup failed. MCP server was not started.");
                    }
                }
            }

            using (new EditorGUI.DisabledScope(!canStop))
            {
                if (GUILayout.Button(new GUIContent("Stop MCP", "Stop the Unity bridge and Python MCP server.")))
                {
                    Mcp.McpServerController.StopAll();
                    _mcpBridgeAvailable = false;
                    _mcpServerAvailable = false;
                    _mcpBridgeChecking = false;
                    _mcpServerChecking = false;
                    _mcpStatusKnown = true;
                    _mcpAutoRetryEnabled = false;
                }
            }

            if (GUILayout.Button(new GUIContent("Check Status", "Refresh bridge and server health checks.")))
            {
                CheckMcpStatusAsync(settings);
            }

            EditorGUILayout.EndHorizontal();
        }

        private static void DrawMcpTools(ChatSettings settings)
        {
            var allTools = Mcp.McpToolRegistry.GetTools(enabledOnly: false);
            var enabledCount = allTools.Count(tool => tool.enabled);

            _mcpToolsExpanded = EditorGUILayout.Foldout(
                _mcpToolsExpanded,
                $"Discoverable Tools ({enabledCount}/{allTools.Count} enabled)",
                true);

            if (!_mcpToolsExpanded)
                return;

            EditorGUILayout.LabelField("Choose which discovered MCP tools are exposed to external MCP clients.", EditorStyles.miniLabel);

            if (allTools.Count == 0)
            {
                EditorGUILayout.HelpBox("No discoverable MCP tools were found.", MessageType.Info);
                return;
            }

            var maxHeight = Mathf.Min(280f, 24f + (allTools.Count * 22f));
            _mcpToolsScroll = EditorGUILayout.BeginScrollView(_mcpToolsScroll, GUILayout.Height(maxHeight));
            foreach (var tool in allTools)
            {
                var currentlyEnabled = settings.IsMcpToolEnabled(tool.name);
                var toggled = EditorGUILayout.ToggleLeft(
                    new GUIContent(tool.name, string.IsNullOrWhiteSpace(tool.description) ? "No description available." : tool.description),
                    currentlyEnabled);
                if (toggled != currentlyEnabled)
                {
                    settings.SetMcpToolEnabled(tool.name, toggled);
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private static void DrawMcpLogs(ChatSettings settings)
        {
            _mcpLogsExpanded = EditorGUILayout.Foldout(_mcpLogsExpanded, "MCP Debug Logs", true);
            if (!_mcpLogsExpanded)
                return;

            if (!settings.McpDebugLogging)
            {
                EditorGUILayout.HelpBox("Turn on MCP Debug Logging to capture bridge and server logs here.", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear Logs", GUILayout.Width(100)))
            {
                Mcp.McpDiagnostics.Clear();
            }
            EditorGUILayout.LabelField("Recent MCP-specific diagnostics captured while logging is enabled.", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            _mcpLogsScroll = EditorGUILayout.BeginScrollView(_mcpLogsScroll, GUILayout.Height(180));
            EditorGUILayout.TextArea(Mcp.McpDiagnostics.GetRecentText(), GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private static bool IsSearchExpectedRunning()
        {
            return _searchAvailable || _searchAutoRetryEnabled;
        }

        private static bool IsMcpExpectedRunning()
        {
            return _mcpBridgeAvailable || _mcpServerAvailable || _mcpAutoRetryEnabled || Mcp.McpServerController.IsHubMode;
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

            // Always ensure MCP dependencies are present, even when the virtualenv already exists.
            return ExtractorRunner.TryCreateMcpEnvironment(
                settings.McpPythonPathResolved,
                settings.McpEnvPathResolved,
                settings.McpPythonFallback);
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
            if (!EnsureSearchEnvironment(settings))
            {
                Debug.LogError("[ChatSettingsProvider] Search environment setup failed. Search server was not started.");
                return;
            }

            bool isAvailable = false;
            var client = GetSearchApiClient(settings);
            if (client != null)
            {
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
                    _searchStatusKnown = false;
                    _searchAutoRetryEnabled = true;
                    _searchRetryUntil = EditorApplication.timeSinceStartup + 10.0;
                    return;
                }
                
                Debug.Log("[ChatSettingsProvider] Search server is already running.");
            }
        }
        
        private static async void StopServerAsync(ChatSettings settings)
        {
            var client = GetSearchApiClient(settings);
            if (client != null)
            {
                try
                {
                    await client.StopSearchServer();
                    Debug.Log("[ChatSettingsProvider] Search server stopped successfully.");
                    _searchAvailable = false;
                    _searchChecking = false;
                    _searchStatusKnown = true;
                    _searchAutoRetryEnabled = false;
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error stopping Search API server: {e.Message}");
                }
            }
        }

        private static async void RebuildSearchIndexAsync(ChatSettings settings)
        {
            if (_searchRebuildInProgress)
                return;

            if (!EnsureSearchEnvironment(settings))
            {
                Debug.LogError("[ChatSettingsProvider] Search environment setup failed. Search index rebuild was not started.");
                return;
            }

            _searchRebuildInProgress = true;
            try
            {
                await System.Threading.Tasks.Task.Run(() => ExtractorRunner.RunExtractor(settings));
                ExtractorRunner.RunIndexer(settings);
                Debug.Log("[ChatSettingsProvider] Search index rebuild started successfully.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[ChatSettingsProvider] Search index rebuild failed: {e.Message}");
            }
            finally
            {
                _searchRebuildInProgress = false;
            }
        }

        private static IIndexingServiceApi GetSearchApiClient(ChatSettings settings)
        {
            var existingWindow = Resources.FindObjectsOfTypeAll<ChatEditorWindow>().FirstOrDefault();
            if (existingWindow != null && existingWindow.SearchApiClient != null)
                return existingWindow.SearchApiClient;

            return new DeepSearchClient(settings.SearchApiHostResolved, settings.SearchApiPythonPathResolved);
        }
    }
}
