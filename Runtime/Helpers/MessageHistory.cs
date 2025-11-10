using System;
using System.Collections.Generic;
using GPTUnity.Data;
using Newtonsoft.Json;
using UnityEngine;

namespace GPTUnity.Helpers
{
    /// <summary>
    /// Manages the history of messages exchanged with the API.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class MessageHistory
    {
        [JsonProperty] 
        private List<GPTMessage> chatHistory = new();

        [JsonIgnore]
        public List<GPTMessage> ChatHistory
        {
            get => chatHistory;
            set => chatHistory = value;
        }

        public void Clear()
        {
            chatHistory.Clear();
        }

        public void Add(GPTMessage message)
        {
            chatHistory.Add(message);
        }
    }
}