using System;
using System.Collections.Generic;
using GPTUnity.Data;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace GPTUnity.Helpers
{
    /// <summary>
    /// Manages the history of messages exchanged with the API.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class MessageHistory
    {
        [JsonIgnore]
        private const string EditorPrefsKey = "AIChatHistory";

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
            chatHistory = new List<GPTMessage>();
            EditorPrefs.DeleteKey(EditorPrefsKey);
        }

        public void Add(GPTMessage message)
        {
            chatHistory.Add(message);
        }
    }
}