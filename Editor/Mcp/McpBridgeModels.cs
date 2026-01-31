using System.Collections.Generic;

namespace Mcp
{
    public class McpToolDefinition
    {
        public string name;
        public string description;
        public object inputSchema;
    }

    public class McpToolCallRequest
    {
        public string name;
        public Dictionary<string, object> arguments;
    }

    public class McpToolCallResponse
    {
        public bool ok;
        public string content;
        public bool isError;
    }
}
