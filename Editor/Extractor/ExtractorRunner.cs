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
                Debug.LogError("Extractor script not found at: " + extractorPath);
                return;
            }

            string assetsPath = Application.dataPath;
            string args = $"\"{extractorPath}\" --project \"{assetsPath}\"";

            ProcessStartInfo startInfo = new ProcessStartInfo(pythonExe, args)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            Process process = new Process();
            process.StartInfo = startInfo;

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Debug.Log("[Extractor] " + e.Data);
            };
            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Debug.LogError("[Extractor ERROR] " + e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            Debug.Log($"Started extraction process:\n{pythonExe} {args}");
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
                Debug.LogError("Extractor script not found at: " + extractorPath);
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

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Debug.Log("[Indexer] " + e.Data);
            };
            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Debug.LogError("[Indexer ERROR] " + e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            Debug.Log($"Started indexing process:\n{pythonExe} {args}");
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
                        process.OutputDataReceived += (sender, e) =>
                        {
                            if (!string.IsNullOrEmpty(e.Data))
                                Debug.Log("[Python venv] " + e.Data);
                        };
                        process.ErrorDataReceived += (sender, e) =>
                        {
                            if (!string.IsNullOrEmpty(e.Data))
                                Debug.LogError("[Python venv ERROR] " + e.Data);
                        };

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
                    
                    var pipInstallArgs = $"-m pip install sentence-transformers==2.7.0 faiss-cpu==1.7.4 fastapi==0.110.0 uvicorn==0.27.1 numpy==1.24.4";
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
                        pipProcess.OutputDataReceived += (sender, e) =>
                        {
                            if (!string.IsNullOrEmpty(e.Data))
                                Debug.Log("[Pip Install] " + e.Data);
                        };
                        pipProcess.ErrorDataReceived += (sender, e) =>
                        {
                            if (!string.IsNullOrEmpty(e.Data))
                                Debug.LogError("[Pip Install ERROR] " + e.Data);
                        };

                        pipProcess.BeginOutputReadLine();
                        pipProcess.BeginErrorReadLine();
                        pipProcess.WaitForExit();
                    }
                    
                    Debug.Log($"VirtualEnv created at {venvFolder}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to create Python environment: {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning("Python environment already exists or path is invalid.");
            }
        }
    }
}