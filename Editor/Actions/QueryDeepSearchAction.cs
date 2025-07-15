using System.Text;
using System.Threading.Tasks;
using GPTUnity.Actions.Interfaces;
using GPTUnity.Indexing;

namespace GPTUnity.Actions
{
    [GPTAction("Performs a deep semantic search over indexed Unity project files and returns paths and summaries of matching assets or code. Only 1 call to this tool per LLM turn allowed")]
    public class QueryDeepSearchAction : GPTAssistantAction, IGPTActionThatContainsCode, IGPTActionThatRequiresIndexingApi
    {
        [GPTParameter("The semantic query to search for within project files (code, docs, assets).")]
        public string Query { get; set; }

        public string Content => Query;

        public IIndexingServiceApi Indexing { get; set; }

        public override async Task<string> Execute()
        {
            if (string.IsNullOrWhiteSpace(Query))
                return "No query provided.";

            var results = await Indexing.SearchAsync(Query, topK: 5, maxResults: 2);
            if (results == null || results.Count == 0)
                return "No relevant assets or files found by deep search.";

            var sb = new StringBuilder();
            sb.AppendLine($"Deep Search Results for: \"{Query}\"");

            foreach (var result in results)
            {
                sb.AppendLine($"Path: {result.file}");
                sb.AppendLine($"Name: {result.name} | Type: {result.type}");
                sb.AppendLine($"Snippet:\n{result.content}");
                sb.AppendLine();
            }

            return sb.ToString();
        }

    }
}