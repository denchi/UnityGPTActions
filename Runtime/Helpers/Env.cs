using System.IO;
using System.Linq;
using UnityEngine;

namespace Game.Environment
{
    public static class Env
    {
        private static bool _envLoaded;
        
        // Automatically load environment variables when the class is first accessed
        static Env()
        {
            LoadEnv();
        }

        private static bool LoadEnv()
        {
            _envLoaded = false;

            var paths = new[]
            {
                Path.Combine(Application.streamingAssetsPath, ".env"),
            };

            var envFilePath = paths.FirstOrDefault(path =>
            {
                return File.Exists(path);
            });

            if (string.IsNullOrEmpty(envFilePath)) 
                return false;
                
            var lines = File.ReadAllLines(envFilePath);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) 
                    continue;
                
                var idx = line.IndexOf('=');
                if (idx < 0)
                {
                    Debug.LogWarning($"Invalid line in .env file: {line}");
                    continue;
                }
                
                var key = line.Substring(0, idx).Trim();
                var value = line.Substring(idx + 1).Trim();
                    
                System.Environment.SetEnvironmentVariable(key, value);
            }
            
            _envLoaded = true;

            return true;
        }
        
        public static string GetEnv(string key)
        {
            if (!_envLoaded)
            {
                LoadEnv();
            }
            
            return System.Environment.GetEnvironmentVariable(key);
        }
        
        public static bool TryGetEnv(string key, out string value)
        {
            value = GetEnv(key);
            return !string.IsNullOrEmpty(value);
        }
    }
}