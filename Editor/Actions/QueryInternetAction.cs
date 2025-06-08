using System;
using System.Net.Http;
using System.Threading.Tasks;
using Game.Environment;
using GPTUnity.Actions.Interfaces;
using Newtonsoft.Json.Linq;

namespace GPTUnity.Actions
{
    [GPTAction("Searches the internet for information related to a query using SerpAPI.")]
    public class QueryInternetAction : GPTAssistantAction, IGPTActionThatContainsCode
    {
        [GPTParameter("The search query to look up on the internet.")]
        public string Query { get; set; }
        
        public string Content => Query;

        // Replace with your actual SerpAPI key
        public override async Task<string> Execute()
        {
            if (string.IsNullOrWhiteSpace(Query))
                return "Query parameter is required.";
            
            if (!Env.TryGetEnv("SERP_API_KEY", out var apiKey))
            {
                throw new Exception("SerpAPI key is not set. Please set the SERP_API_KEY environment variable.");
            }

            var url = $"https://serpapi.com/search.json?q={Uri.EscapeDataString(Query)}&api_key={apiKey}&num=1";

            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                    return $"Failed to search the internet: {response.ReasonPhrase}";

                var json = await response.Content.ReadAsStringAsync();
                var jObj = JObject.Parse(json);

                var organicResults = jObj["organic_results"] as JArray;
                if (organicResults != null && organicResults.Count > 0)
                {
                    var firstResult = organicResults[0];
                    var title = firstResult["title"]?.ToString() ?? "";
                    var snippet = firstResult["snippet"]?.ToString() ?? "";
                    var link = firstResult["link"]?.ToString() ?? "";
                    return $"{title}\n{snippet}\n{link}";
                }
                
                return "No results found.";
            }
        }
    }
}
