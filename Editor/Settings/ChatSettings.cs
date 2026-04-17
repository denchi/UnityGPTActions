using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using System.IO;

namespace GPTUnity.Settings
{
    [FilePath("ProjectSettings/ChatSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class ChatSettings : ScriptableSingleton<ChatSettings>
    {
        [SerializeField] private Color colorBackgroundUser = new Color(0.3098f, 0.3098f, 0.3098f, 1f);
        [SerializeField] private Color colorBackgroundAssistant = new Color(0.251f, 0.251f, 0.251f, 1f);
        [SerializeField] private Color colorChatBackground = new Color(0.1686f, 0.1686f, 0.1686f, 1f);
        
        [Header("Search Api Settings")]
        [SerializeField] private string _searchApiHost = "http://127.0.0.1:8000";
        [SerializeField] private bool _searchApiAutoHost = true;

        [Header("Python Settings")]
        [SerializeField] private string _pythonPath = "venv_mcp/bin/python3";
        [SerializeField] private string _envPath = "venv_mcp";
        [SerializeField] private string _pythonFallback = "python3";
        
        [Header("MCP Settings")]
        [SerializeField] private string _mcpBridgeUrl = "http://127.0.0.1:7071";
        [SerializeField] private bool _mcpBridgeAutoUrl = true;
        [SerializeField] private string _mcpServerUrl = "http://127.0.0.1:7072/mcp";
        [SerializeField] private bool _mcpAutoStart = true;
        [SerializeField] private bool _mcpUseUpdateQueue = true;

        public Color ColorBackgroundUser
        {
            get => colorBackgroundUser;
            set => colorBackgroundUser = value;
        }

        public Color ColorBackgroundAssistant
        {
            get => colorBackgroundAssistant;
            set => colorBackgroundAssistant = value;
        }

        public Color ColorChatBackground
        {
            get => colorChatBackground;
            set => colorChatBackground = value;
        }

        public string SearchApiHost
        {
            get { return _searchApiHost; }
            set { _searchApiHost = value; }
        }

        public bool SearchApiAutoHost
        {
            get { return _searchApiAutoHost; }
            set { _searchApiAutoHost = value; }
        }

        public string SearchApiPythonPath
        {
            get { return _pythonPath; }
            set { _pythonPath = value; }
        }

        public string SearchApiEnvPath
        {
            get { return _envPath; }
            set { _envPath = value; }
        }

        public string SearchApiPythonFallback
        {
            get { return _pythonFallback; }
            set { _pythonFallback = value; }
        }

        public string McpBridgeUrl
        {
            get { return _mcpBridgeUrl; }
            set { _mcpBridgeUrl = value; }
        }

        public bool McpBridgeAutoUrl
        {
            get { return _mcpBridgeAutoUrl; }
            set { _mcpBridgeAutoUrl = value; }
        }

        public string McpServerUrl
        {
            get { return _mcpServerUrl; }
            set { _mcpServerUrl = value; }
        }

        public string McpPythonPath
        {
            get { return _pythonPath; }
            set { _pythonPath = value; }
        }

        public bool McpAutoStart
        {
            get { return _mcpAutoStart; }
            set { _mcpAutoStart = value; }
        }

        public bool McpUseUpdateQueue
        {
            get { return _mcpUseUpdateQueue; }
            set { _mcpUseUpdateQueue = value; }
        }

        public string McpEnvPath
        {
            get { return _envPath; }
            set { _envPath = value; }
        }

        public string McpPythonFallback
        {
            get { return _pythonFallback; }
            set { _pythonFallback = value; }
        }

        public string SearchApiPythonPathResolved => ResolveLibraryPyPath(_pythonPath);
        public string SearchApiEnvPathResolved => ResolveLibraryPyPath(_envPath);
        public string SearchApiHostResolved => LocalServiceEndpointResolver.ResolveSearchApiHost(this);
        public string McpBridgeUrlResolved => LocalServiceEndpointResolver.ResolveMcpBridgeUrl(this);
        public string McpPythonPathResolved => ResolveLibraryPyPath(_pythonPath);
        public string McpEnvPathResolved => ResolveLibraryPyPath(_envPath);

        public static string NormalizeLibraryPyRelative(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            var trimmed = input.Trim().Replace("\\", "/");
            if (trimmed.Equals("Library/py", System.StringComparison.OrdinalIgnoreCase))
                return string.Empty;

            if (trimmed.StartsWith("Library/py/", System.StringComparison.OrdinalIgnoreCase))
                return trimmed.Substring("Library/py/".Length);

            if (System.IO.Path.IsPathRooted(trimmed))
            {
                var fullPath = System.IO.Path.GetFullPath(trimmed).Replace("\\", "/");
                var root = GetLibraryPyRoot().Replace("\\", "/");
                if (fullPath.StartsWith(root, System.StringComparison.OrdinalIgnoreCase))
                {
                    return fullPath.Substring(root.Length).TrimStart('/');
                }

                Debug.LogWarning($"[ChatSettings] Expected a path under '{root}'. Storing relative name only.");
                return System.IO.Path.GetFileName(fullPath);
            }

            return trimmed.TrimStart('/');
        }

        public static string ResolveLibraryPyPath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return relativePath;

            var normalized = relativePath.Replace("\\", "/").TrimStart('/');
            if (System.IO.Path.IsPathRooted(normalized))
                return System.IO.Path.GetFullPath(normalized);
            if (normalized.Equals("Library/py", System.StringComparison.OrdinalIgnoreCase))
                return GetLibraryPyRoot();

            if (normalized.StartsWith("Library/py/", System.StringComparison.OrdinalIgnoreCase))
                normalized = normalized.Substring("Library/py/".Length);

            return System.IO.Path.Combine(GetLibraryPyRoot(), normalized);
        }

        public static string GetLibraryPyRoot()
        {
            return System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "Library", "py"));
        }

        public void SaveSettings(bool saveAsText = true)
        {
            Save(saveAsText);
        }
    }
}
