using System;
using System.Collections.Generic;

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
        
        public static IReadOnlyCollection<GPTMessage> CreateUserMessage(string content)
        {
            return new List<GPTMessage>
            {
                new GPTMessage
                {
                    role = "user",
                    content = content
                }
            };
        }
        
        public static IReadOnlyCollection<GPTMessage> CreateUserMessage(string systemMessage, string content)
        {
            return new List<GPTMessage>
            {
                new GPTMessage
                {
                    role = "system",
                    content = systemMessage
                },
                new GPTMessage
                {
                    role = "user",
                    content = content
                }
            };
        }
    }
}