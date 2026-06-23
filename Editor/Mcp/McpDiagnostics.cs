using System;
using System.Collections.Generic;
using System.Text;
using GPTUnity.Settings;
using UnityEngine;

namespace Mcp
{
    public static class McpDiagnostics
    {
        private const int MaxEntries = 300;
        private static readonly List<string> Entries = new List<string>(MaxEntries);
        private static readonly object LockObj = new object();

        public static void Log(string message)
        {
            if (!IsEnabled())
                return;

            Append("INFO", message);
        }

        public static void LogError(string message)
        {
            if (!IsEnabled())
                return;

            Append("ERROR", message);
        }

        public static void Clear()
        {
            lock (LockObj)
            {
                Entries.Clear();
            }
        }

        public static string GetRecentText(int maxLines = 80)
        {
            lock (LockObj)
            {
                if (Entries.Count == 0)
                    return "No MCP debug logs captured yet.";

                var startIndex = Mathf.Max(0, Entries.Count - Mathf.Max(1, maxLines));
                var builder = new StringBuilder();
                for (var i = startIndex; i < Entries.Count; i++)
                {
                    builder.AppendLine(Entries[i]);
                }

                return builder.ToString().TrimEnd();
            }
        }

        private static bool IsEnabled()
        {
            var settings = ChatSettings.instance;
            return settings != null && settings.McpDebugLogging;
        }

        private static void Append(string level, string message)
        {
            lock (LockObj)
            {
                if (Entries.Count >= MaxEntries)
                {
                    Entries.RemoveAt(0);
                }

                Entries.Add($"[{DateTime.Now:HH:mm:ss}] [{level}] {message ?? string.Empty}");
            }
        }
    }
}
