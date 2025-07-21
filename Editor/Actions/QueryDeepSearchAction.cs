using System.Text;
using System.Threading.Tasks;
using GPTUnity.Actions.Interfaces;
using GPTUnity.Indexing;

namespace GPTUnity.Actions
{
    [GPTAction("Searches project files (code, docs, assets) using a semantic query.")]
    public class QueryDeepSearchAction : GPTAssistantAction, IGPTActionThatContainsCode, IGPTActionThatRequiresIndexingApi
    {
        [GPTParameter("The semantic query to search for within project files (code, docs, assets).")]
        public string Query { get; set; }

        public string Content => Query;
        
        #region IGPTActionThatRequiresIndexingApi Implementation 

        public IIndexingServiceApi Indexing { get; set; }
        
        #endregion

        public override async Task<string> Execute()
        {
            if (string.IsNullOrWhiteSpace(Query))
                return "<b>No query provided.</b>";

            var results = await Indexing.SearchAsync(Query, topK: 5, maxResults: 2);
            if (results == null || results.Count == 0)
                return "<b>No relevant assets or files found by deep search.</b>";

            var sb = new StringBuilder();
            sb.AppendLine($"<b>Deep Search Results for: \"{EscapeRich(Query)}\"</b>");
            sb.AppendLine("\n");

            foreach (var result in results)
            {
                var snippet = result.content;
                // if (!string.IsNullOrEmpty(snippet) && snippet.Length > 300)
                //     snippet = snippet.Substring(0, 300) + "...";
                
                sb.AppendLine("<b>Path:</b> " + EscapeRich(result.file));
                sb.AppendLine("<b>Type:</b> " + EscapeRich(result.type));
                sb.AppendLine("<b>Name:</b> " + EscapeRich(result.name));
                if (!string.IsNullOrEmpty(result.className))
                    sb.AppendLine("<b>Class:</b> " + EscapeRich(result.className));
                sb.AppendLine(HighlightCSharp(snippet));
                sb.AppendLine("\n");
            }

            return sb.ToString();
        }

        // Escapes Unity rich text special characters
        string EscapeRich(string text)
        {
            if (string.IsNullOrEmpty(text)) 
                return "";
                
            return text;//.Replace("<", "&lt;").Replace(">", "&gt;");
        }

        // Very basic C# syntax highlighter for Unity Rich Text
        string HighlightCSharp(string code)
        {
            if (string.IsNullOrEmpty(code)) return "";

            // Escape rich text
            code = EscapeRich(code);

            // Highlight keywords (add more as needed)
            string[] keywords = {
                "public", "private", "protected", "class", "void", "int", "string", "float", "bool",
                "return", "if", "else", "for", "while", "switch", "case", "using", "namespace", "new",
                "var", "static", "async", "await", "null", "true", "false"
            };
            foreach (var kw in keywords)
                code = System.Text.RegularExpressions.Regex.Replace(
                    code, $@"\b{kw}\b", $"<color=#569CD6>{kw}</color>");

            // Highlight types (add more as needed)
            string[] types = { "Task", "StringBuilder" };
            foreach (var type in types)
                code = System.Text.RegularExpressions.Regex.Replace(
                    code, $@"\b{type}\b", $"<color=#4EC9B0>{type}</color>");

            // Highlight strings
            code = System.Text.RegularExpressions.Regex.Replace(
                code, "\"([^\"]*)\"", "<color=#D69D85>\"$1\"</color>");

            // Highlight comments
            code = System.Text.RegularExpressions.Regex.Replace(
                code, @"(//.*?$)", "<color=#57A64A>$1</color>", System.Text.RegularExpressions.RegexOptions.Multiline);

            return $"<b><color=#CCCCCC><size=12>{code}</size></color></b>";
        }
    }
}