using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using System.Net.Http;
using System.Diagnostics;
using System.IO;
using GPTUnity.Indexing;
using Debug = UnityEngine.Debug;

namespace GPTUnity.Indexing
{
    public class DeepSearchClient : IIndexingServiceApi
    {
        private const string PackageId = "com.deathbygravitystudio.gptactions";
        // private const string SearchApiScript = "Packages/" + PackageId + "/Editor/Extractor/search_api.py";
        
        private readonly string _host;
        private readonly string _pythonExe;
        
        // Paths relative to unity project root

        private Process _serverProcess;
        
        public DeepSearchClient(string host = "http://127.0.0.1:8000", string pythonExe = "venv/bin/python3")
        {
            this._host = host;
            this._pythonExe = pythonExe;
        }

        public async Task<bool> IsServerAvailable()
        {
            using var client = new HttpClient();
            var response = await client.GetAsync(_host + "/ping");
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> StartSearchServerAsync()
        {
            if (_serverProcess != null && !_serverProcess.HasExited)
            {
                Debug.Log("[DeepSearchClient] Search server is already running.");
                return false;
            }
            
            var packageRoot = Path.GetFullPath($"Packages/{PackageId}");
            var searchApiScript = Path.Combine(packageRoot, "Editor/Extractor/search_api.py");

            var startInfo = new ProcessStartInfo(_pythonExe, $"\"{searchApiScript}\"")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            _serverProcess = new Process();
            _serverProcess.StartInfo = startInfo;
            _serverProcess.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    UnityEngine.Debug.Log("[SearchAPI] " + e.Data);
            };
            _serverProcess.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    UnityEngine.Debug.LogError("[SearchAPI ERROR] " + e.Data);
            };

            _serverProcess.Start();
            _serverProcess.BeginOutputReadLine();
            _serverProcess.BeginErrorReadLine();

            Debug.Log("[DeepSearchClient] Started local search API server, waiting for readiness...");

            const int maxAttempts = 20;
            const int delayMs = 500;
            
            Exception _lastException = null;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                try
                {
                    var isServerAvailable = await IsServerAvailable();
                    if (isServerAvailable)
                        return true;
                }
                catch (Exception e)
                {
                    // Ignore exceptions during the check, they will be logged later
                    _lastException = e;
                }

                await Task.Delay(delayMs);
            }

            Debug.LogError($"[DeepSearchClient] Failed to start Search API {_lastException?.Message ?? "after multiple attempts"}.");

            return false;
        }

        public Task<bool> StopSearchServer()
        {
            if (_serverProcess == null || _serverProcess.HasExited)
            {
                Debug.Log("[DeepSearchClient] Search server is not running.");
                return Task.FromResult(false);
            }

            try
            {
                _serverProcess.Kill();
                _serverProcess.Dispose();
                _serverProcess = null;
                Debug.Log("[DeepSearchClient] Search server stopped.");
                return Task.FromResult(true);
            }
            catch (Exception e)
            {
                Debug.LogError($"[DeepSearchClient] Failed to stop search server: {e.Message}");
                return Task.FromResult(false);
            }
        }

        public async Task<List<SearchResult>> SearchAsync(string query, int maxResults = 10, int topK = 5)
        {
            var isServerAvailable = false;

            try
            {
                isServerAvailable = await IsServerAvailable();
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
            }
            
            if (!isServerAvailable)
            {
                await StartSearchServerAsync();
            }

            using var client = new HttpClient();
            var requestBody = new SearchRequest
            {
                query = query,
                top_k = topK,
                max_results = maxResults
            };

            string json = JsonUtility.ToJson(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync(_host + "/search", content);

                if (!response.IsSuccessStatusCode)
                {
                    Debug.LogError($"[DeepSearchClient] Failed with status code: {response.StatusCode}");
                    return new List<SearchResult>();
                }

                string responseJson = await response.Content.ReadAsStringAsync();
                string wrappedJson = "{\"results\":" + responseJson + "}";
                var resultList = JsonUtility.FromJson<SearchResultList>(wrappedJson);
                return resultList.results;
            }
            catch (Exception e)
            {
                Debug.LogError($"[DeepSearchClient] HTTP request failed: {e.Message}");
                return new List<SearchResult>();
            }
        }
    }
}
