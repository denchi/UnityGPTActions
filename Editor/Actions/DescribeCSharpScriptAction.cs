using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace GPTUnity.Actions
{
    [GPTAction("Describes the contents of a C# script file.")]
    public class DescribeCSharpScriptAction : GPTActionBase
    {
        [GPTParameter("Path to the C# script file")]
        public string Input { get; set; }

        public override string Content => _result;
        public override string Description => _description;

        private string _result = "";
        private string _description = "";

        public override void Execute()
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
            AnalyzeScript(sourceCode);
        }

        private void AnalyzeScript(string sourceCode)
        {
            var sb = new StringBuilder();
            var descSb = new StringBuilder();

            // Extract namespace
            var namespaceMatch = Regex.Match(sourceCode, @"namespace\s+([^\s{]+)");
            string namespaceName = namespaceMatch.Success ? namespaceMatch.Groups[1].Value : "No namespace";

            // Extract classes
            var classMatches = Regex.Matches(sourceCode, @"(?:public|private|protected|internal)?\s*class\s+(\w+)");
            
            // Extract methods
            var methodMatches = Regex.Matches(sourceCode, @"(?:public|private|protected|internal)?\s+(?:static\s+)?(?:<[^>]+>\s+)?[\w<>[\]]+\s+(\w+)\s*\([^)]*\)");

            // Build description
            descSb.AppendLine($"Analysis of {Path.GetFileName(Input)}:");
            descSb.AppendLine($"- Namespace: {Highlight(namespaceName)}");
            descSb.AppendLine($"- Classes found: {Highlight(classMatches.Count.ToString())}");
            descSb.AppendLine($"- Methods found: {Highlight(methodMatches.Count.ToString())}");

            // Build detailed content
            sb.AppendLine($"C# Script Analysis - {Path.GetFileName(Input)}");
            sb.AppendLine($"Namespace: {namespaceName}");
            sb.AppendLine("\nClasses:");
            foreach (Match classMatch in classMatches)
            {
                sb.AppendLine($"- {classMatch.Groups[1].Value}");
            }

            sb.AppendLine("\nMethods:");
            foreach (Match methodMatch in methodMatches)
            {
                sb.AppendLine($"- {methodMatch.Groups[1].Value}");
            }

            _description = descSb.ToString();
            _result = sb.ToString();
        }
    }
}
