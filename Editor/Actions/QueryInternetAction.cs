using System;
using System.Net.Http;
using System.Threading.Tasks;
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
        private const string SerpApiKey = "3462f64e2ab5e52c43075a5529a68cd6c5c28d8784d7dbad0fb8ebf5e560ef55";

        public override async Task<string> Execute()
        {
            if (string.IsNullOrWhiteSpace(Query))
                return "Query parameter is required.";

            var url = $"https://serpapi.com/search.json?q={Uri.EscapeDataString(Query)}&api_key={SerpApiKey}&num=1";

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
