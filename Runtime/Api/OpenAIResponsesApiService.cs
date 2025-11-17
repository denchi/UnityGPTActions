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
    public class OpenAIResponsesApiService : IGPTServiceApi
    {
        private readonly int _maxTokens;
        private readonly string _key;

        public OpenAIResponsesApiService(string key, int maxTokens = 8192)
        {
            _key = key;
            _maxTokens = maxTokens;
        }

        public IReadOnlyList<string> Models => new List<string>
        {
            "gpt-5",
            "gpt-4.1",
            "gpt-4.1-mini",
            "gpt-4.1-nano",
            "gpt-4o",
            "gpt-4",
            "gpt-3.5-turbo"
        };

        public async Task<GPTFunctionResponse> Chat(IReadOnlyCollection<GPTMessage> messages, string model, object[] tools = null, object schema = null)
        {
            var client = new HttpClient();
            var url = "https://api.openai.com/v1/responses";

            var serializer = JsonSerializer.Create(new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            var inputMessages = messages.Select(MapInputMessage).ToList();

            var requestBody = new JObject
            {
                ["model"] = model,
                ["input"] = JToken.FromObject(inputMessages, serializer),
                ["max_output_tokens"] = _maxTokens
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

            Debug.Log($"<color=blue>[Network]</color> Sending request to {url} with model {model} and input: {requestJson}");

            var response = await client.PostAsync(url, content);
            var responseJson = await response.Content.ReadAsStringAsync();

            Debug.Log($"\t<color=blue>[Network]</color> {url} => {responseJson}");

            if (responseJson.Contains("\"error\""))
            {
                throw new Exception(responseJson);
            }

            var parsed = JsonConvert.DeserializeObject<OpenAIResponsesApiResponse>(responseJson);
            return ConvertResponse(parsed);
        }

        public async Task<T> Get<T>(IReadOnlyCollection<GPTMessage> messages, string model, object schema, object[] tools = null)
        {
            var result = await Chat(messages, model, tools, schema);
            if (result.choices == null || result.choices.Count == 0)
            {
                throw new Exception("No choices returned from the model.");
            }

            return JsonConvert.DeserializeObject<T>(result.choices[0].message.StringContent);
        }

        private static JObject MapInputMessage(GPTMessage message)
        {
            var content = message.content?.Select(MapInputContent).ToList();

            var payload = new JObject
            {
                ["role"] = message.role
            };

            if (content != null && content.Count > 0)
            {
                payload["content"] = JToken.FromObject(content);
            }

            if (!string.IsNullOrEmpty(message.tool_call_id))
            {
                payload["tool_call_id"] = message.tool_call_id;
            }

            if (!string.IsNullOrEmpty(message.name))
            {
                payload["name"] = message.name;
            }

            return payload;
        }

        private static GPTMessage.Content MapInputContent(GPTMessage.Content content)
        {
            var mappedType = content.type switch
            {
                "text" => "input_text",
                _ => content.type
            };

            return new GPTMessage.Content
            {
                type = mappedType,
                text = mappedType == "input_text" ? content.text : null,
                input_audio = mappedType == "input_audio" ? content.input_audio : null
            };
        }

        private static GPTFunctionResponse ConvertResponse(OpenAIResponsesApiResponse response)
        {
            if (response?.output == null || response.output.Count == 0)
            {
                return new GPTFunctionResponse
                {
                    choices = new List<GPTChoice>()
                };
            }

            var outputMessage = response.output.First();

            var gptMessage = new GPTMessage
            {
                role = outputMessage.role,
                content = outputMessage.content?.Select(MapContent).ToList(),
                tool_calls = outputMessage.tool_calls
            };

            return new GPTFunctionResponse
            {
                choices = new List<GPTChoice>
                {
                    new GPTChoice
                    {
                        message = gptMessage,
                        finish_reason = MapFinishReason(response.stop_reason)
                    }
                }
            };
        }

        private static FinishReason MapFinishReason(string stopReason)
        {
            return stopReason switch
            {
                "tool_call" => FinishReason.tool_calls,
                "max_output_tokens" => FinishReason.length,
                "content_filter" => FinishReason.content_filter,
                "function_call" => FinishReason.function_call,
                _ => FinishReason.stop
            };
        }

        private static GPTMessage.Content MapContent(OpenAIResponseContent content)
        {
            return new GPTMessage.Content
            {
                type = content.type == "output_text" ? "text" : content.type,
                text = content.text
            };
        }

        public class OpenAIResponsesApiResponse
        {
            public string id { get; set; }
            public string @object { get; set; }
            public string model { get; set; }
            public long created { get; set; }
            public List<OpenAIResponseMessage> output { get; set; }
            public string stop_reason { get; set; }
        }

        public class OpenAIResponseMessage
        {
            public string role { get; set; }
            public List<OpenAIResponseContent> content { get; set; }
            public GPTToolCall[] tool_calls { get; set; }
        }

        public class OpenAIResponseContent
        {
            public string type { get; set; }
            public string text { get; set; }
        }
    }
}