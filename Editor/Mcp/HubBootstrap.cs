using GPTUnity.Settings;
using UnityEngine;

namespace Mcp
{
    public static class HubBootstrap
    {
        // Unity CLI entrypoint: -executeMethod Mcp.HubBootstrap.Start
        public static void Start()
        {
            var settings = ChatSettings.instance;
            if (settings == null)
            {
                Debug.LogError("[MCP] HubBootstrap failed: ChatSettings.instance is null.");
                return;
            }

            if (McpServerController.StartAll(settings))
            {
                Debug.Log("[MCP] HubBootstrap started MCP services.");
                return;
            }

            Debug.LogError("[MCP] HubBootstrap failed to start MCP services.");
        }
    }
}
