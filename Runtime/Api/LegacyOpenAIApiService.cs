using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GPTUnity.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace GPTUnity.Api
{
    public class LegacyOpenAIApiService : IGPTServiceApi
    {
        private readonly int _maxTokens;
        private readonly string _key;
        private List<string> _cachedModels;
        private bool _modelsFetched = false;
        
        public LegacyOpenAIApiService(string key, int maxTokens = 8192)
        {
            _key = key;
            _maxTokens = maxTokens;
        }

        public IReadOnlyList<string> Models 
        {
            get
            {
                if (!_modelsFetched)
                {
                    // Return fallback models if not fetched yet
                    return GetFallbackModels();
                }
                return _cachedModels ?? GetFallbackModels();
            }
        }

        public async Task<GPTFunctionResponse> Chat(IReadOnlyCollection<GPTMessage> messages, string model, object[] tools = null, object schema = null)
        {
            var client = new HttpClient();
            var url = "https://api.openai.com/v1/chat/completions";
            
            var requestBody = new JObject
            {
                ["model"] = model,
                ["messages"] = JToken.FromObject(messages),
            };

            // Use appropriate token parameter based on model
            var isNewer = IsNewerModel(model);
            if (isNewer)
            {
                requestBody["max_completion_tokens"] = _maxTokens;
            }
            else
            {
                requestBody["max_tokens"] = _maxTokens;
            }

            if (tools != null)
            {
                requestBody["tool_choice"] = "auto";
                requestBody["tools"] = JToken.FromObject(tools);

            }
            else
            {
                Debug.LogWarning("<color=yellow>[API]</color> No tools provided to Chat method!");
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
            
            Debug.Log($"<color=blue>[Network]</color> Sending request to {url} with model {model} using {(isNewer ? "max_completion_tokens" : "max_tokens")} parameter");


            var response = await client.PostAsync(url, content);
            var responseJson = await response.Content.ReadAsStringAsync();
            
            Debug.Log($"\t<color=blue>[Network]</color> {url} => {responseJson}");
            
            if (responseJson.Contains("\"error\""))
            {
                // Check if it's the token parameter error and retry with alternative parameter
                if (responseJson.Contains("max_tokens") && responseJson.Contains("max_completion_tokens"))
                {
                    Debug.LogWarning($"<color=yellow>[Network]</color> Token parameter error detected for model {model}. Retrying with alternative parameter...");
                    
                    // Retry with the opposite token parameter
                    var retryRequestBody = new JObject
                    {
                        ["model"] = model,
                        ["messages"] = JToken.FromObject(messages),
                    };

                    // Use the opposite parameter from what we tried first
                    if (isNewer)
                    {
                        retryRequestBody["max_tokens"] = _maxTokens;
                    }
                    else
                    {
                        retryRequestBody["max_completion_tokens"] = _maxTokens;
                    }

                    if (tools != null)
                    {
                        retryRequestBody["tool_choice"] = "auto";
                        retryRequestBody["tools"] = JToken.FromObject(tools);
                    }
                    
                    if (schema != null)
                    {
                        retryRequestBody["response_format"] = JToken.FromObject(new
                        {
                            type = "json_schema",
                            json_schema = JToken.FromObject(schema)
                        });
                    }

                    var retryRequestJson = JsonConvert.SerializeObject(retryRequestBody);
                    var retryContent = new StringContent(retryRequestJson, Encoding.UTF8, "application/json");
                    
                    var retryClient = new HttpClient();
                    retryClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_key}");
                    
                    Debug.Log($"<color=blue>[Network]</color> Retry request with {(!isNewer ? "max_completion_tokens" : "max_tokens")} parameter");
                    
                    var retryResponse = await retryClient.PostAsync(url, retryContent);
                    var retryResponseJson = await retryResponse.Content.ReadAsStringAsync();
                    
                    Debug.Log($"\t<color=blue>[Network]</color> Retry {url} => {retryResponseJson}");
                    
                    if (retryResponseJson.Contains("\"error\""))
                    {
                        throw new Exception(retryResponseJson);
                    }
                    
                    return JsonConvert.DeserializeObject<GPTFunctionResponse>(retryResponseJson);
                }
                
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
            if (result.choices == null || result.choices.Count == 0)
            {
                throw new Exception("No choices returned from the model.");
            }
            
            return JsonConvert.DeserializeObject<T>(result.choices[0].message.StringContent);
        }

        private List<string> GetFallbackModels()
        {
            return new List<string>
            {
                "gpt-4o",
                "gpt-4o-mini", 
                "gpt-4-turbo",
                "gpt-4",
                "gpt-3.5-turbo"
            };
        }

        /// <summary>
        /// Fetches available models from OpenAI API and caches them
        /// </summary>
        public async Task FetchAvailableModelsAsync()
        {
            try
            {
                var client = new HttpClient();
                var url = "https://api.openai.com/v1/models";
                
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_key}");
                
                var response = await client.GetAsync(url);
                var responseJson = await response.Content.ReadAsStringAsync();
                
                if (responseJson.Contains("\"error\""))
                {
                    Debug.LogWarning($"Error fetching models: {responseJson}. Using fallback models.");
                    _cachedModels = GetFallbackModels();
                }
                else
                {
                    var modelsResponse = JsonConvert.DeserializeObject<OpenAIModelsResponse>(responseJson);
                    
                    // Filter for GPT models and sort by preference
                    var gptModels = modelsResponse.data
                        .Where(m => m.id.StartsWith("gpt-") && m.id.Contains("gpt"))
                        .Select(m => m.id)
                        .OrderByDescending(m => GetModelPriority(m))
                        .ToList();
                    
                    _cachedModels = gptModels.Any() ? gptModels : GetFallbackModels();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to fetch models from OpenAI API: {ex.Message}. Using fallback models.");
                _cachedModels = GetFallbackModels();
            }
            finally
            {
                _modelsFetched = true;
            }
        }

        private int GetModelPriority(string modelId)
        {
            // Priority order for models (higher number = higher priority)
            if (modelId.Contains("gpt-4o")) return 100;
            if (modelId.Contains("gpt-4-turbo")) return 90;
            if (modelId.Contains("gpt-4")) return 80;
            if (modelId.Contains("gpt-3.5-turbo")) return 70;
            return 0;
        }

        private bool IsNewerModel(string model)
        {
            // Models that require max_completion_tokens instead of max_tokens
            // GPT-5+ models and some newer GPT-4 variants
            var modelLower = model.ToLowerInvariant();
            
            // Future GPT versions
            if (modelLower.StartsWith("gpt-5") ||
                modelLower.StartsWith("gpt-6") ||
                modelLower.StartsWith("gpt-7") ||
                modelLower.StartsWith("gpt-8") ||
                modelLower.StartsWith("gpt-9"))
            {
                return true;
            }
            
            // Specific newer GPT-4 variants that use the new parameter
            if (modelLower.Contains("gpt-4o-2024") ||
                modelLower.Contains("gpt-4-turbo-2024"))
            {
                return true;
            }
            
            // Development/preview models often use new parameters
            if (modelLower.Contains("preview") ||
                modelLower.Contains("alpha") ||
                modelLower.Contains("beta") ||
                modelLower.Contains("snapshot"))
            {
                return true;
            }
            
            return false;
        }
    }

    // Response classes for OpenAI Models API
    [Serializable]
    public class OpenAIModelsResponse
    {
        public string @object { get; set; }
        public List<OpenAIModel> data { get; set; }
    }

    [Serializable]
    public class OpenAIModel
    {
        public string id { get; set; }
        public string @object { get; set; }
        public long created { get; set; }
        public string owned_by { get; set; }
    }
}