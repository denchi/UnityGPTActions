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
        // Path to your Python3 executable inside your env
        // private static readonly string pythonExe = "/Users/denis/dev/ChatGptAssistant/Library/py/search/bin/python3";

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
        
        public static void TryCreatePythonEnvironment(string pythonPath, string envPath = "Library/py/mcp", string pythonFallback = "python3")
        {
            CreateOrUpdatePythonEnvironment(
                pythonPath,
                defaultVenvName: envPath,
                packages: new[]
                {
                    "sentence-transformers==2.7.0",
                    "faiss-cpu==1.7.4",
                    "starlette==0.49.1",
                    "uvicorn==0.31.1",
                    "numpy==1.24.4",
                    "tree_sitter==0.20.4"
                },
                requirePython310: false,
                includeMcp: false,
                pythonFallback: pythonFallback);
        }

        public static void TryCreateMcpEnvironment(string pythonPath, string envPath = "Library/py/mcp", string pythonFallback = "python3.11")
        {
            CreateOrUpdatePythonEnvironment(
                pythonPath,
                defaultVenvName: envPath,
                packages: new[]
                {
                    "mcp==1.26.0"
                },
                requirePython310: true,
                includeMcp: true,
                pythonFallback: pythonFallback);
        }

        private static void CreateOrUpdatePythonEnvironment(
            string pythonPath,
            string defaultVenvName,
            string[] packages,
            bool requirePython310,
            bool includeMcp,
            string pythonFallback)
        {
            if (string.IsNullOrWhiteSpace(pythonPath))
            {
                Debug.LogError("[Indexer] Python path is empty.");
                return;
            }

            var pythonFile = ResolvePythonExecutable(pythonPath);
            if (!IsSimpleExecutableName(pythonFile) && !File.Exists(pythonFile))
            {
                var fallback = ResolveInterpreterFallback(requirePython310, pythonFallback);
                Debug.LogWarning($"[Indexer] Python path not found: {pythonFile}. Falling back to '{fallback}'.");
                pythonFile = fallback;
            }
            var venvFolder = ResolveVenvFolder(pythonPath, defaultVenvName);

            if (string.IsNullOrEmpty(venvFolder))
            {
                Debug.LogError("[Indexer] Invalid Python path. Expected something like Library/py/search/bin/python3 or python3.11.");
                return;
            }

            var shouldCreateVenv = !Directory.Exists(venvFolder);

            try
            {
                if (shouldCreateVenv)
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = pythonFile,
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
                }

                var venvPython = ResolveVenvPython(pythonPath, venvFolder);
                if (requirePython310 && !PythonSupportsMcp(venvPython))
                {
                    Debug.LogWarning("[Indexer] MCP requires Python >= 3.10. Configure MCP Python Path to a 3.10+ interpreter.");
                    return;
                }

                var pipPackages = new System.Collections.Generic.List<string>(packages);
                if (includeMcp && !pipPackages.Contains("mcp==1.26.0"))
                {
                    pipPackages.Add("mcp==1.26.0");
                }

                var pipInstallArgs = $"-m pip install {string.Join(" ", pipPackages)}";

                var pipStartInfo = new ProcessStartInfo
                {
                    FileName = venvPython,
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

                var status = shouldCreateVenv ? "created and configured" : "updated";
                Debug.Log($"[Indexer] Python environment {status} at {venvFolder}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Indexer] Failed to configure Python environment: {e.Message}");
            }
        }

        private static bool PythonSupportsMcp(string pythonFile)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = pythonFile,
                    Arguments = "-c \"import sys; print(f'{sys.version_info.major}.{sys.version_info.minor}')\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(startInfo))
                {
                    process.WaitForExit();
                    var output = process.StandardOutput.ReadToEnd().Trim();
                    if (string.IsNullOrEmpty(output))
                        return false;

                    if (Version.TryParse(output, out var version))
                    {
                        return version.Major > 3 || (version.Major == 3 && version.Minor >= 10);
                    }
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private static string ResolvePythonExecutable(string pythonPath)
        {
            if (IsSimpleExecutableName(pythonPath))
                return pythonPath;

            return Path.GetFullPath(pythonPath);
        }

        private static string ResolveVenvFolder(string pythonPath, string defaultVenvName)
        {
            if (IsSimpleExecutableName(pythonPath))
            {
                return Path.GetFullPath(defaultVenvName);
            }

            var pythonFile = Path.GetFullPath(pythonPath);
            var pythonDir = Directory.GetParent(pythonFile);
            return pythonDir?.Parent?.FullName;
        }

        private static string ResolveVenvPython(string pythonPath, string venvFolder)
        {
            if (IsSimpleExecutableName(pythonPath))
            {
                return Path.Combine(venvFolder, "bin", "python3");
            }
            
            var resolved = Path.GetFullPath(pythonPath);
            if (resolved.StartsWith(venvFolder, StringComparison.Ordinal) && !File.Exists(resolved))
            {
                return Path.Combine(venvFolder, "bin", "python3");
            }

            return resolved;
        }

        private static bool IsSimpleExecutableName(string pythonPath)
        {
            return pythonPath.IndexOf(Path.DirectorySeparatorChar) == -1 &&
                   pythonPath.IndexOf(Path.AltDirectorySeparatorChar) == -1;
        }

        private static string DeriveInterpreterFallback(bool requirePython310)
        {
            return requirePython310 ? "python3.11" : "python3";
        }

        private static string ResolveInterpreterFallback(bool requirePython310, string pythonFallback)
        {
            if (!string.IsNullOrWhiteSpace(pythonFallback))
            {
                if (IsSimpleExecutableName(pythonFallback))
                    return pythonFallback;
                if (File.Exists(pythonFallback))
                    return pythonFallback;
            }

            foreach (var candidate in GetInterpreterCandidates(requirePython310))
            {
                if (IsSimpleExecutableName(candidate))
                {
                    if (TestInterpreter(candidate, requirePython310))
                        return candidate;
                }
                else if (File.Exists(candidate) && TestInterpreter(candidate, requirePython310))
                {
                    return candidate;
                }
            }

            return DeriveInterpreterFallback(requirePython310);
        }

        private static string[] GetInterpreterCandidates(bool requirePython310)
        {
            if (requirePython310)
            {
                return new[]
                {
                    "python3.11",
                    "python3.12",
                    "python3.10",
                    "/opt/homebrew/bin/python3.11",
                    "/opt/homebrew/bin/python3.12",
                    "/usr/local/bin/python3.11",
                    "/usr/local/bin/python3.12"
                };
            }

            return new[]
            {
                "python3",
                "python",
                "/opt/homebrew/bin/python3",
                "/usr/local/bin/python3"
            };
        }

        private static bool TestInterpreter(string pythonExe, bool requirePython310)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = pythonExe,
                    Arguments = "-c \"import sys; print(f'{sys.version_info.major}.{sys.version_info.minor}')\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(startInfo))
                {
                    process.WaitForExit(2000);
                    var output = process.StandardOutput.ReadToEnd().Trim();
                    if (string.IsNullOrEmpty(output))
                        return false;

                    if (Version.TryParse(output, out var version))
                    {
                        return !requirePython310 || version.Major > 3 || (version.Major == 3 && version.Minor >= 10);
                    }
                }
            }
            catch
            {
                return false;
            }

            return false;
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
