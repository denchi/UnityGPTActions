using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Game.Environment;
using GPTUnity.Actions.Interfaces;
using Newtonsoft.Json.Linq;

namespace GPTUnity.Actions
{
    public enum AssetStoreSearchProvider
    {
        Auto,
        GoogleCse,
        SerpApi
    }

    [GPTAction("Finds Unity Asset Store packages by query using Google CSE or SerpAPI.")]
    public class QueryAssetStoreAction : GPTAssistantAction, IGPTActionThatContainsCode
    {
        [GPTParameter("Search terms for the Unity asset package.", true)]
        public string Query { get; set; }

        [GPTParameter("Provider to use: Auto, GoogleCse, or SerpApi.")]
        public AssetStoreSearchProvider Provider { get; set; } = AssetStoreSearchProvider.Auto;

        [GPTParameter("Maximum number of results to return (1-10).")]
        public int MaxResults { get; set; } = 5;

        [GPTParameter("Start index for paged results (1-based).")]
        public int StartIndex { get; set; } = 1;

        [GPTParameter("Include marketplace.unity.com/packages in addition to assetstore.unity.com/packages.")]
        public bool IncludeMarketplace { get; set; } = true;

        [GPTParameter("Optional Google API key override. If empty, uses GOOGLE_CSE_API_KEY or GOOGLE_API_KEY.")]
        public string GoogleApiKey { get; set; }

        [GPTParameter("Optional Google CSE ID override. If empty, uses GOOGLE_CSE_CX or GOOGLE_SEARCH_CX.")]
        public string GoogleCseId { get; set; }

        [GPTParameter("Optional SerpAPI key override. If empty, uses SERP_API_KEY.")]
        public string SerpApiKey { get; set; }

        public string Content => Query;

        public override async Task<string> Execute()
        {
            if (string.IsNullOrWhiteSpace(Query))
                return "Query parameter is required.";

            var maxResults = Clamp(MaxResults, 1, 10, 5);
            var startIndex = StartIndex <= 0 ? 1 : StartIndex;
            var scopedQuery = BuildScopedQuery(Query, IncludeMarketplace);
            var errors = new List<string>();

            if (Provider == AssetStoreSearchProvider.GoogleCse || Provider == AssetStoreSearchProvider.Auto)
            {
                var googleResult = await SearchWithGoogleCse(scopedQuery, maxResults, startIndex, IncludeMarketplace);
                if (googleResult.Items.Count > 0)
                    return FormatResults("Google CSE", googleResult.Items);

                if (!string.IsNullOrEmpty(googleResult.Error))
                    errors.Add(googleResult.Error);
            }

            if (Provider == AssetStoreSearchProvider.SerpApi || Provider == AssetStoreSearchProvider.Auto)
            {
                var serpResult = await SearchWithSerpApi(scopedQuery, maxResults, startIndex, IncludeMarketplace);
                if (serpResult.Items.Count > 0)
                    return FormatResults("SerpAPI", serpResult.Items);

                if (!string.IsNullOrEmpty(serpResult.Error))
                    errors.Add(serpResult.Error);
            }

            var errorText = errors.Count > 0 ? "\n" + string.Join("\n", errors) : string.Empty;
            return $"No Asset Store results found for \"{Query}\".{errorText}";
        }

        private async Task<SearchProviderResult> SearchWithGoogleCse(
            string scopedQuery,
            int maxResults,
            int startIndex,
            bool includeMarketplace)
        {
            if (!TryResolveGoogleCredentials(out var apiKey, out var cx))
            {
                return SearchProviderResult.Fail(
                    "Google CSE is not configured. Set GOOGLE_CSE_API_KEY (or GOOGLE_API_KEY) and GOOGLE_CSE_CX (or GOOGLE_SEARCH_CX).");
            }

            var url =
                $"https://www.googleapis.com/customsearch/v1?key={Uri.EscapeDataString(apiKey)}&cx={Uri.EscapeDataString(cx)}&q={Uri.EscapeDataString(scopedQuery)}&start={startIndex}&num={maxResults}";

            try
            {
                using (var client = CreateHttpClient())
                using (var response = await client.GetAsync(url))
                {
                    var json = await response.Content.ReadAsStringAsync();
                    if (!response.IsSuccessStatusCode)
                        return SearchProviderResult.Fail(
                            $"Google CSE search failed: {(int)response.StatusCode} {response.ReasonPhrase}. {CompactBody(json)}");

                    var root = JObject.Parse(json);
                    var items = ParseGoogleItems(root, includeMarketplace, maxResults);
                    return SearchProviderResult.Success(items);
                }
            }
            catch (Exception ex)
            {
                return SearchProviderResult.Fail($"Google CSE search failed: {ex.Message}");
            }
        }

        private async Task<SearchProviderResult> SearchWithSerpApi(
            string scopedQuery,
            int maxResults,
            int startIndex,
            bool includeMarketplace)
        {
            var apiKey = GetFirstNonEmpty(SerpApiKey, Env.GetEnv("SERP_API_KEY"));
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return SearchProviderResult.Fail(
                    "SerpAPI is not configured. Set SERP_API_KEY.");
            }

            var url =
                $"https://serpapi.com/search.json?engine=google&q={Uri.EscapeDataString(scopedQuery)}&api_key={Uri.EscapeDataString(apiKey)}&num={maxResults}&start={startIndex - 1}";

            try
            {
                using (var client = CreateHttpClient())
                using (var response = await client.GetAsync(url))
                {
                    var json = await response.Content.ReadAsStringAsync();
                    if (!response.IsSuccessStatusCode)
                        return SearchProviderResult.Fail(
                            $"SerpAPI search failed: {(int)response.StatusCode} {response.ReasonPhrase}. {CompactBody(json)}");

                    var root = JObject.Parse(json);
                    var items = ParseSerpItems(root, includeMarketplace, maxResults);
                    return SearchProviderResult.Success(items);
                }
            }
            catch (Exception ex)
            {
                return SearchProviderResult.Fail($"SerpAPI search failed: {ex.Message}");
            }
        }

        private bool TryResolveGoogleCredentials(out string apiKey, out string cx)
        {
            apiKey = GetFirstNonEmpty(
                GoogleApiKey,
                Env.GetEnv("GOOGLE_CSE_API_KEY"),
                Env.GetEnv("GOOGLE_API_KEY"));

            cx = GetFirstNonEmpty(
                GoogleCseId,
                Env.GetEnv("GOOGLE_CSE_CX"),
                Env.GetEnv("GOOGLE_SEARCH_CX"));

            return !string.IsNullOrWhiteSpace(apiKey) && !string.IsNullOrWhiteSpace(cx);
        }

        private static HttpClient CreateHttpClient()
        {
            return new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        }

        private static string BuildScopedQuery(string query, bool includeMarketplace)
        {
            var siteFilter = includeMarketplace
                ? "(site:assetstore.unity.com/packages OR site:marketplace.unity.com/packages)"
                : "site:assetstore.unity.com/packages";

            return $"{query} {siteFilter}";
        }

        private static List<SearchResultItem> ParseGoogleItems(JObject root, bool includeMarketplace, int maxResults)
        {
            var items = new List<SearchResultItem>();
            var jsonItems = root["items"] as JArray;
            if (jsonItems == null)
                return items;

            foreach (var item in jsonItems.OfType<JObject>())
            {
                var link = item["link"]?.ToString();
                if (!IsAssetStorePackageLink(link, includeMarketplace))
                    continue;

                items.Add(new SearchResultItem
                {
                    Title = item["title"]?.ToString() ?? "Untitled",
                    Link = link,
                    Snippet = item["snippet"]?.ToString() ?? string.Empty
                });
            }

            return items.Take(maxResults).ToList();
        }

        private static List<SearchResultItem> ParseSerpItems(JObject root, bool includeMarketplace, int maxResults)
        {
            var items = new List<SearchResultItem>();
            var jsonItems = root["organic_results"] as JArray;
            if (jsonItems == null)
                return items;

            foreach (var item in jsonItems.OfType<JObject>())
            {
                var link = item["link"]?.ToString();
                if (!IsAssetStorePackageLink(link, includeMarketplace))
                    continue;

                items.Add(new SearchResultItem
                {
                    Title = item["title"]?.ToString() ?? "Untitled",
                    Link = link,
                    Snippet = item["snippet"]?.ToString() ?? string.Empty
                });
            }

            return items.Take(maxResults).ToList();
        }

        private static bool IsAssetStorePackageLink(string link, bool includeMarketplace)
        {
            if (string.IsNullOrWhiteSpace(link) || !Uri.TryCreate(link, UriKind.Absolute, out var uri))
                return false;

            if (!uri.AbsolutePath.StartsWith("/packages", StringComparison.OrdinalIgnoreCase))
                return false;

            var host = uri.Host.ToLowerInvariant();
            if (host.Equals("assetstore.unity.com", StringComparison.Ordinal) ||
                host.EndsWith(".assetstore.unity.com", StringComparison.Ordinal))
                return true;

            return includeMarketplace &&
                   (host.Equals("marketplace.unity.com", StringComparison.Ordinal) ||
                    host.EndsWith(".marketplace.unity.com", StringComparison.Ordinal));
        }

        private static string FormatResults(string providerName, IEnumerable<SearchResultItem> items)
        {
            var list = items.ToList();
            if (list.Count == 0)
                return "No Asset Store results found.";

            var lines = new List<string>
            {
                $"Asset Store results ({providerName}):"
            };

            for (var i = 0; i < list.Count; i++)
            {
                var item = list[i];
                lines.Add($"{i + 1}. {item.Title}");
                if (!string.IsNullOrWhiteSpace(item.Snippet))
                    lines.Add($"   {item.Snippet}");
                lines.Add($"   {item.Link}");
            }

            return string.Join("\n", lines);
        }

        private static string GetFirstNonEmpty(params string[] values)
        {
            return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
        }

        private static string CompactBody(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var compact = text.Replace('\n', ' ').Replace('\r', ' ').Trim();
            if (compact.Length > 400)
                compact = compact.Substring(0, 400) + "...";

            return compact;
        }

        private static int Clamp(int value, int min, int max, int fallback)
        {
            if (value <= 0)
                value = fallback;

            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }

        private sealed class SearchResultItem
        {
            public string Title { get; set; }
            public string Link { get; set; }
            public string Snippet { get; set; }
        }

        private sealed class SearchProviderResult
        {
            public List<SearchResultItem> Items { get; }
            public string Error { get; }

            private SearchProviderResult(List<SearchResultItem> items, string error)
            {
                Items = items ?? new List<SearchResultItem>();
                Error = error;
            }

            public static SearchProviderResult Success(List<SearchResultItem> items)
            {
                return new SearchProviderResult(items, null);
            }

            public static SearchProviderResult Fail(string error)
            {
                return new SearchProviderResult(new List<SearchResultItem>(), error);
            }
        }
    }
}
