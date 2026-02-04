using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GPTUnity.Helpers;
using UnityEngine;

namespace GPTUnity.Actions
{
    [GPTAction("Retrieves the last N Unity console logs, optionally filtered by type.")]
    public class RetrieveUnityLogsAction : GPTAssistantAction
    {
        [GPTParameter("Number of log entries to return (default 50).")]
        public int Count { get; set; } = 50;

        [GPTParameter("Log type filter: Any, Log, Warning, Error, Assert, Exception.")]
        public string TypeFilter { get; set; } = "Any";

        [GPTParameter("Include stack traces in the output.")]
        public bool IncludeStackTrace { get; set; } = false;

        public override async Task<string> Execute()
        {
#if UNITY_EDITOR
            var filter = ParseFilter(TypeFilter);
            var logs = EditorLogBuffer.GetLogs(filter, Count);

            if (logs.Count == 0)
                return "No logs found for the requested filter.";

            var sb = new StringBuilder();
            sb.AppendLine($"Returned {logs.Count} log(s){(filter == null ? "" : $" of type {filter}")}:");

            foreach (var entry in logs.AsEnumerable().Reverse())
            {
                sb.AppendLine($"[{entry.Timestamp:HH:mm:ss}] {entry.Type}: {entry.Message}");
                if (IncludeStackTrace && !string.IsNullOrWhiteSpace(entry.StackTrace))
                {
                    sb.AppendLine(entry.StackTrace);
                }
            }

            return sb.ToString();
#else
            return "This action can only be run in the Unity Editor.";
#endif
        }

        private static LogType? ParseFilter(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            var normalized = raw.Trim();
            if (string.Equals(normalized, "Any", StringComparison.OrdinalIgnoreCase))
                return null;

            if (Enum.TryParse(normalized, true, out LogType parsed))
                return parsed;

            return null;
        }
    }
}
