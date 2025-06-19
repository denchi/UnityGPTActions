using System;
using System.Collections.Generic;
using GPTUnity.Data;
using Newtonsoft.Json;
using UnityEngine;

namespace GPTUnity.Helpers
{
    /// <summary>
    /// Keeps track of executed tool calls to avoid duplicates. 
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class ToolCallsHistory
    {
        [JsonProperty] 
        private List<string> _toolCallsIds = new();

        public void Clear()
        {
            _toolCallsIds.Clear();
        }

        public void MarkToolCallExecuted(GPTToolCall toolCall)
        {
            if (!IsToolCallExecuted(toolCall))
            {
                Debug.Log($"Tool call {toolCall.id} executed!");
                _toolCallsIds.Add(toolCall.id);
            }
        }

        public bool IsToolCallExecuted(GPTToolCall toolCall)
        {
            return _toolCallsIds.Contains(toolCall.id);
        }
    }
}
