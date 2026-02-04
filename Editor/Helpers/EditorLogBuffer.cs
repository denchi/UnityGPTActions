using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GPTUnity.Helpers
{
    [InitializeOnLoad]
    public static class EditorLogBuffer
    {
        private const int MaxEntries = 1000;
        private static readonly List<LogEntry> Entries = new List<LogEntry>(MaxEntries);
        private static readonly object LockObj = new object();

        static EditorLogBuffer()
        {
            Application.logMessageReceived += HandleLogMessage;
        }

        private static void HandleLogMessage(string condition, string stackTrace, LogType type)
        {
            lock (LockObj)
            {
                if (Entries.Count >= MaxEntries)
                {
                    Entries.RemoveAt(0);
                }

                Entries.Add(new LogEntry
                {
                    Timestamp = DateTime.Now,
                    Type = type,
                    Message = condition ?? string.Empty,
                    StackTrace = stackTrace ?? string.Empty
                });
            }
        }

        public static List<LogEntry> GetLogs(LogType? filter, int count)
        {
            if (count <= 0)
                return new List<LogEntry>();

            var results = new List<LogEntry>(Mathf.Min(count, MaxEntries));

            lock (LockObj)
            {
                for (var i = Entries.Count - 1; i >= 0 && results.Count < count; i--)
                {
                    var entry = Entries[i];
                    if (filter == null || entry.Type == filter.Value)
                    {
                        results.Add(entry);
                    }
                }
            }

            return results;
        }

        public struct LogEntry
        {
            public DateTime Timestamp;
            public LogType Type;
            public string Message;
            public string StackTrace;
        }
    }
}
