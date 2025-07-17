using System;
using UnityEditor;
using UnityEngine;
using System.Diagnostics;
using System.IO;
using Debug = UnityEngine.Debug;
using GPTUnity.Settings; // Add this for ChatSettings

namespace DeathByGravity.GPTActions
{
    public static class ExtractorRunner
    {
        // Path to your Python3 executable inside your venv
        // private static readonly string pythonExe = "/Users/denis/dev/ChatGptAssistant/venv/bin/python3";

        [MenuItem("Tools/Unity Assistant/Run Extractor")]
        public static void RunExtractor(ChatSettings settings)
        {
            string pythonExe = settings.SearchApiPythonPath;
            string extractorPath = Path.GetFullPath(
                "Packages/com.deathbygravitystudio.gptactions/Editor/Extractor/extract_all.py"
            );

            if (!File.Exists(extractorPath))
            {
                Debug.LogError("[Indexer] " + "Extractor script not found at: " + extractorPath);
                return;
            }

            string assetsPath = Application.dataPath;
            
            string dataPath = Path.GetFullPath("Packages/com.deathbygravitystudio.gptactions/Editor/Extractor/prebuilt");
            if (!Directory.Exists(dataPath))
            {
                Debug.LogError("[Indexer] " + "Extractor script not found at: " + extractorPath);
                return;
            }

            var args = $"\"{extractorPath}\" --project \"{assetsPath}\" --data \"{dataPath}\"";

            var startInfo = new ProcessStartInfo(pythonExe, args)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            var process = new Process();
            process.StartInfo = startInfo;

            process.OutputDataReceived += Log;
            process.ErrorDataReceived += LogError;

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            Debug.Log($"[Indexer] " + "Started extraction process:\n{pythonExe} {args}");
            
            // Wait for the process to exit before returning
            process.WaitForExit();
        }
        
        [MenuItem("Tools/Unity Assistant/Run Indexer")]
        public static void RunIndexer(ChatSettings settings)
        {
            string pythonExe = settings.SearchApiPythonPath;
            string extractorPath = Path.GetFullPath(
                "Packages/com.deathbygravitystudio.gptactions/Editor/Extractor/index_all.py"
            );

            if (!File.Exists(extractorPath))
            {
                Debug.LogError("[Indexer] " + "Extractor script not found at: " + extractorPath);
                return;
            }

            string assetsPath = Application.dataPath;
            string args = $"\"{extractorPath}\"";

            ProcessStartInfo startInfo = new ProcessStartInfo(pythonExe, args)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            Process process = new Process();
            process.StartInfo = startInfo;

            process.OutputDataReceived += Log;
            process.ErrorDataReceived += LogError;

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            Debug.Log($"[Indexer] " + "Started indexing process:\n{pythonExe} {args}");
        }

        // [MenuItem("Tools/Unity Assistant/Start Server")]
        // public static void StartServer()
        // {
        //     _ = DeepSearchClient.StartSearchServerAsync();
        // }
        //
        // [MenuItem("Tools/Unity Assistant/Stop Server")]
        // public static void StopServer()
        // {
        //     DeepSearchClient.StopSearchServer();
        // }
        
        public static void TryCreatePythonEnvironment(string pythonPath)
        {
            var venvFolder = Path.Combine(Directory.GetParent(pythonPath).Parent.FullName); // This should point to /venv

            if (!string.IsNullOrEmpty(venvFolder) && !Directory.Exists(venvFolder))
            {
                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = "python3", // or "python" on Windows
                        Arguments = $"-m venv \"{venvFolder}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    using (var process = Process.Start(startInfo))
                    {
                        process.OutputDataReceived += Log;
                        process.ErrorDataReceived += LogError;

                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();
                        process.WaitForExit();
                    }
                    
                    // Install pip packages here
                    // sentence-transformers==2.7.0
                    // faiss-cpu==1.7.4
                    // fastapi==0.110.0
                    // uvicorn==0.27.1
                    // numpy==1.24.4
                    
                    var pipPackages = new[]
                    {
                        "sentence-transformers==2.7.0",
                        "faiss-cpu==1.7.4",
                        "fastapi==0.110.0",
                        "uvicorn==0.27.1",
                        "numpy==1.24.4",
                        "tree_sitter==0.20.4"
                    };

                    var pipInstallArgs = $"-m pip install {string.Join(" ", pipPackages)}";

                    var pipStartInfo = new ProcessStartInfo
                    {
                        FileName = Path.Combine(venvFolder, "bin", "python3"), // Adjust for Windows if needed
                        Arguments = pipInstallArgs,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };
                    
                    using (var pipProcess = Process.Start(pipStartInfo))
                    {
                        pipProcess.OutputDataReceived += Log;
                        pipProcess.ErrorDataReceived += LogError;

                        pipProcess.BeginOutputReadLine();
                        pipProcess.BeginErrorReadLine();
                        pipProcess.WaitForExit();
                    }
                    
                    Debug.Log($"[Indexer] " + "VirtualEnv created at {venvFolder}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Indexer] " + "Failed to create Python environment: {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning("[Indexer] " + "Python environment already exists or path is invalid.");
            }
        }
        
        private static void Log(object sender, DataReceivedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Data))
                return;
                
            Debug.Log("[Indexer] " + e.Data);
        }
    
        private static void LogError(object sender, DataReceivedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Data))
                return;
                
            if (e.Data.ToLower().Contains("warn"))
                Debug.LogWarning("[Indexer] " + e.Data);
            else
                Debug.LogError("[Indexer] " + e.Data);
        }
    }
}