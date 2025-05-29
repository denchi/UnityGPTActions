using System;

namespace GPTUnity.Data
{
    [Serializable]
    public class GPTMessage
    {
        public string role;
        public string content;

        // Tools Responses
        public GPTToolCall[] tool_calls;

        // Tools Completions
        public string tool_call_id;
        public string name;
    }
}