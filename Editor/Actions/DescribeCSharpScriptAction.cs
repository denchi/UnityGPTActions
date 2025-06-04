using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GPTUnity.Actions
{
    [GPTAction("Describes the contents of a C# script file.")]
    public class DescribeCSharpScriptAction : GPTAssistantAction
    {
        [GPTParameter("Path to the C# script file")]
        public string Input { get; set; }

        // public override string Description => _description;
        //
        // private string _result = "";
        // private string _description = "";

        public override async Task<string> Execute()
        {
            if (string.IsNullOrEmpty(Input))
            {
                throw new Exception("No file path provided.");
            }

            string filePath = Input.Trim();
            if (!File.Exists(filePath))
            {
                throw new Exception($"File not found: {filePath}");
            }

            string sourceCode = File.ReadAllText(filePath);
            
            return AnalyzeScript(sourceCode);
        }

        private string AnalyzeScript(string sourceCode)
        {
            var sb = new StringBuilder();

            // Extract namespace
            var namespaceMatch = Regex.Match(sourceCode, @"namespace\s+([^\s{]+)");
            string namespaceName = namespaceMatch.Success ? namespaceMatch.Groups[1].Value : "No namespace";

            // Extract classes and their parents
            var classMatches = Regex.Matches(sourceCode, @"(?:public|private|protected|internal)?\s*class\s+(\w+)(?:\s*:\s*([^{\s]+))?");
            
            // Extract methods
            var methodMatches = Regex.Matches(sourceCode, @"(?:public|private|protected|internal)?\s+(?:static\s+)?(?:<[^>]+>\s+)?[\w<>[\]]+\s+(\w+)\s*\([^)]*\)");

            // Build detailed content
            sb.AppendLine($"C# Script Analysis - {Path.GetFileName(Input)}");
            sb.AppendLine($"Namespace: {namespaceName}");
            sb.AppendLine("\nClasses:");
            foreach (Match classMatch in classMatches)
            {
                string className = classMatch.Groups[1].Value;
                string parentClass = classMatch.Groups[2].Success ? classMatch.Groups[2].Value : "None";
                sb.AppendLine($"- {className} (Parent: {parentClass})");
            }

            sb.AppendLine("\nMethods:");
            foreach (Match methodMatch in methodMatches)
            {
                sb.AppendLine($"- {methodMatch.Groups[1].Value}");
            }

            return sb.ToString();
        }
    }
}
