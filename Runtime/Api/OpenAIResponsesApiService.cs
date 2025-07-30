using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GPTUnity.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace GPTUnity.Api
{
    // public class ResponsesOpenAIApiService : IGPTServiceApi
    // {
    //     private readonly int _maxTokens;
    //     private readonly string _key;
    //     
    //     public ResponsesOpenAIApiService(string key, int maxTokens = 8192)
    //     {
    //         _key = key;
    //         _maxTokens = maxTokens;
    //     }
    //
    //     public IReadOnlyList<string> Models => new List<string>
    //     {
    //         "gpt-3.5-turbo", 
    //         "gpt-4", 
    //         "gpt-4o", 
    //         "gpt-4.1", 
    //         "gpt-4.1-mini", 
    //         "gpt-4.1-nano"
    //     };
    //
    //     public async Task<GPTFunctionResponse> Chat(IReadOnlyCollection<GPTMessage> messages, string model, object[] tools = null, object schema = null)
    //     {
    //         var client = new HttpClient();
    //         var url = "https://api.openai.com/v1/responses";
    //
    //         // Convert GPTMessage list to input array
    //         var input = new JArray();
    //         foreach (var msg in messages)
    //         {
    //             input.Add(new JObject
    //             {
    //                 ["type"] = "input_text",
    //                 ["role"] = msg.role, // "user", "system", etc.
    //                 ["text"] = msg.content
    //             });
    //         }
    //
    //         var requestBody = new JObject
    //         {
    //             ["model"] = model,
    //             ["input"] = input
    //         };
    //
    //         if (tools != null)
    //         {
    //             requestBody["tools"] = JToken.FromObject(tools);
    //         }
    //         
    //         if (schema != null)
    //         {
    //             requestBody["response_format"] = JToken.FromObject(new
    //             {
    //                 type = "json_schema",
    //                 json_schema = JToken.FromObject(schema)
    //             });
    //         }
    //
    //         var requestJson = requestBody.ToString();
    //         var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
    //         client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_key}");
    //
    //         var response = await client.PostAsync(url, content);
    //         var responseJson = await response.Content.ReadAsStringAsync();
    //
    //         if (responseJson.Contains("\"error\""))
    //         {
    //             throw new Exception(responseJson);
    //         }
    //
    //         // Parse using the new model
    //         var parsed = JsonConvert.DeserializeObject<GPTResponseApiResponse>(responseJson);
    //
    //         // Convert to legacy-style response so your app can still use it
    //         return new GPTFunctionResponse
    //         {
    //             choices = new List<GPTChoice>
    //             {
    //                 new GPTChoice
    //                 {
    //                     message = new GPTMessage
    //                     {
    //                         role = "assistant",
    //                         content = parsed.output_text
    //                     },
    //                     finish_reason = FinishReason.stop
    //                 }
    //             }
    //         };
    //     }
    //
    //     /// <summary>
    //     /// Calls the LLM with a given prompt and attempts to deserialize the response into the specified type.
    //     /// </summary>
    //     /// <param name="messages"></param>
    //     /// <param name="model"></param>
    //     /// <param name="schema"></param>
    //     /// <param name="tools"></param>
    //     /// <typeparam name="T"></typeparam>
    //     /// <returns></returns>
    //     /// <exception cref="Exception"></exception>
    //     public async Task<T> Get<T>(IReadOnlyCollection<GPTMessage> messages, string model, object schema, object[] tools = null)
    //     {
    //         var result = await Chat(messages, model, tools, schema);
    //         if (result.choices == null || result.choices.Count == 0)
    //         {
    //             throw new Exception("No choices returned from the model.");
    //         }
    //         
    //         return JsonConvert.DeserializeObject<T>(result.choices[0].message.content);
    //     }
    //
    //     public class GPTResponseApiResponse
    //     {
    //         public string id { get; set; }
    //         public string @object { get; set; }
    //         public string model { get; set; }
    //         public long created { get; set; }
    //         public string output_text { get; set; }
    //     }
    // }
}