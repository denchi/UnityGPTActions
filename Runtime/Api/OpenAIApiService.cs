using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GPTUnity.Data;
using Newtonsoft.Json;

namespace GPTUnity.Api
{
    public class OpenAIApiService : IGPTServiceApi
    {
        private const int MaxTokens = 8192;
        private string key;
        
        public OpenAIApiService(string key)
        {
            this.key = key;
        }

        public IReadOnlyList<string> GetModels()
        {
            return new List<string> { "gpt-3.5-turbo", "gpt-4", "gpt-4o", "gpt-4.1", "gpt-4.1-mini", "gpt-4.1-nano" };
        }
        
        public async Task<GPTFunctionResponse> Chat(IReadOnlyCollection<GPTMessage> messages, string model, object[] tools = null)
        {
            var client = new HttpClient();
            var url = "https://api.openai.com/v1/chat/completions";
            var requestBody = new
            {
                model,
                messages,
                tools,
                tool_choice = "auto",
                max_tokens = MaxTokens
            };
            
            var requestJson = JsonConvert.SerializeObject(requestBody);
            
            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {key}");

            var response = await client.PostAsync(url, content);
            var responseJson = await response.Content.ReadAsStringAsync();
            
            if (responseJson.Contains("\"error\""))
            {
                throw new Exception(responseJson);
            }
            
            return JsonConvert.DeserializeObject<GPTFunctionResponse>(responseJson);
        }
    }
}