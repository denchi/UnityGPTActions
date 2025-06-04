using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GPTUnity.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sirenix.Utilities;
using UnityEngine;

namespace GPTUnity.Api
{
    public class LegacyOpenAIApiService : IGPTServiceApi
    {
        private readonly int _maxTokens;
        private readonly string _key;
        
        public LegacyOpenAIApiService(string key, int maxTokens = 8192)
        {
            _key = key;
            _maxTokens = maxTokens;
        }

        public IReadOnlyList<string> Models => new List<string>
        {
            "gpt-3.5-turbo", 
            "gpt-4", 
            "gpt-4o", 
            "gpt-4.1", 
            "gpt-4.1-mini", 
            "gpt-4.1-nano"
        };

        public async Task<GPTFunctionResponse> Chat(IReadOnlyCollection<GPTMessage> messages, string model, object[] tools = null, object schema = null)
        {
            var client = new HttpClient();
            var url = "https://api.openai.com/v1/chat/completions";
            
            var requestBody = new JObject
            {
                ["model"] = model,
                ["messages"] = JToken.FromObject(messages),
                ["max_tokens"] = _maxTokens,
            };

            if (tools != null)
            {
                requestBody["tool_choice"] = "auto";
                requestBody["tools"] = JToken.FromObject(tools);
            }
            
            if (schema != null)
            {
                requestBody["response_format"] = JToken.FromObject(new
                {
                    type = "json_schema",
                    json_schema = JToken.FromObject(schema)
                });
            }

            var requestJson = JsonConvert.SerializeObject(requestBody);
            
            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_key}");
            
            Debug.Log($"<color=blue>[Network]</color> Sending request to {url} with model {model} and messages: {requestJson}");

            var response = await client.PostAsync(url, content);
            var responseJson = await response.Content.ReadAsStringAsync();
            
            Debug.Log($"\t<color=blue>[Network]</color> {url} => {responseJson}");
            
            if (responseJson.Contains("\"error\""))
            {
                throw new Exception(responseJson);
            }
            
            return JsonConvert.DeserializeObject<GPTFunctionResponse>(responseJson);
        }

        /// <summary>
        /// Calls the LLM with a given prompt and attempts to deserialize the response into the specified type.
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="model"></param>
        /// <param name="schema"></param>
        /// <param name="tools"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<T> Get<T>(IReadOnlyCollection<GPTMessage> messages, string model, object schema, object[] tools = null)
        {
            var result = await Chat(messages, model, tools, schema);
            if (result.choices.IsNullOrEmpty())
            {
                throw new Exception("No choices returned from the model.");
            }
            
            return JsonConvert.DeserializeObject<T>(result.choices[0].message.content);
        }
    }
}