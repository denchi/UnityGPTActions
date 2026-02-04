using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

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
        [SerializeField] private string _searchApiPythonPath = "Library/py/search/bin/python3";
        [SerializeField] private string _searchApiEnvPath = "Library/py/search";
        [SerializeField] private string _searchApiPythonFallback = "python3";
        
        [Header("MCP Settings")]
        [SerializeField] private string _mcpBridgeUrl = "http://127.0.0.1:7071";
        [SerializeField] private string _mcpServerUrl = "http://127.0.0.1:7072/mcp/sse";
        [SerializeField] private string _mcpPythonPath = "Library/py/mcp/bin/python3";
        [SerializeField] private bool _mcpAutoStart = false;
        [SerializeField] private string _mcpEnvPath = "Library/py/mcp";
        [SerializeField] private string _mcpPythonFallback = "python3.11";

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

        public string SearchApiPythonPath
        {
            get { return _searchApiPythonPath; }
            set { _searchApiPythonPath = value; }
        }

        public string SearchApiEnvPath
        {
            get { return _searchApiEnvPath; }
            set { _searchApiEnvPath = value; }
        }

        public string SearchApiPythonFallback
        {
            get { return _searchApiPythonFallback; }
            set { _searchApiPythonFallback = value; }
        }

        public string McpBridgeUrl
        {
            get { return _mcpBridgeUrl; }
            set { _mcpBridgeUrl = value; }
        }

        public string McpServerUrl
        {
            get { return _mcpServerUrl; }
            set { _mcpServerUrl = value; }
        }

        public string McpPythonPath
        {
            get { return _mcpPythonPath; }
            set { _mcpPythonPath = value; }
        }

        public bool McpAutoStart
        {
            get { return _mcpAutoStart; }
            set { _mcpAutoStart = value; }
        }

        public string McpEnvPath
        {
            get { return _mcpEnvPath; }
            set { _mcpEnvPath = value; }
        }

        public string McpPythonFallback
        {
            get { return _mcpPythonFallback; }
            set { _mcpPythonFallback = value; }
        }
    }
}
