using System;
using System.Net.Http;
using System.Threading.Tasks;
using Game.Environment;
using GPTUnity.Actions.Interfaces;
    using Newtonsoft.Json.Linq;

namespace GPTUnity.Actions
{
    [GPTAction("Searches the web for lightweight external research using SerpAPI. Use this for supplemental context, not authoritative Unity project state.", Name = "search_web")]
    public class QueryInternetAction : GPTAssistantAction, IGPTActionThatContainsCode
    {
        [GPTParameter("Search query to look up on the web.", true, Name = "query")]
        public string Query { get; set; }

        [GPTParameter("Maximum number of organic search results to return (1-10).", Name = "max_results")]
        public int MaxResults { get; set; } = 3;
        
        public string Content => Query;

        public override async Task<string> Execute()
        {
            if (string.IsNullOrWhiteSpace(Query))
                return "Query parameter is required.";
            
            if (!Env.TryGetEnv("SERP_API_KEY", out var apiKey))
            {
                throw new Exception("SerpAPI key is not set. Please set the SERP_API_KEY environment variable.");
            }

            var maxResults = Math.Max(1, Math.Min(MaxResults, 10));
            var url = $"https://serpapi.com/search.json?q={Uri.EscapeDataString(Query)}&api_key={apiKey}&num={maxResults}";

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
                    var lines = new System.Collections.Generic.List<string>
                    {
                        $"Web results for \"{Query}\":"
                    };

                    for (var i = 0; i < organicResults.Count && i < maxResults; i++)
                    {
                        var result = organicResults[i];
                        var title = result["title"]?.ToString() ?? "Untitled";
                        var snippet = result["snippet"]?.ToString() ?? string.Empty;
                        var link = result["link"]?.ToString() ?? string.Empty;
                        lines.Add($"{i + 1}. {title}");
                        if (!string.IsNullOrWhiteSpace(snippet))
                            lines.Add($"   {snippet}");
                        if (!string.IsNullOrWhiteSpace(link))
                            lines.Add($"   {link}");
                    }

                    return string.Join("\n", lines);
                }
                
                return "No results found.";
            }
        }
    }
}
