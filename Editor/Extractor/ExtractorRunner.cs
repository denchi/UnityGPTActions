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
        public static void RunExtractor()
        {
            RunExtractor(ChatSettings.instance);
        }

        public static void RunExtractor(ChatSettings settings)
        {
            if (settings == null)
            {
                Debug.LogError("[Indexer] ChatSettings.instance is null.");
                return;
            }

            string pythonExe = settings.SearchApiPythonPathResolved;
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
        public static void RunIndexer()
        {
            RunIndexer(ChatSettings.instance);
        }

        public static void RunIndexer(ChatSettings settings)
        {
            if (settings == null)
            {
                Debug.LogError("[Indexer] ChatSettings.instance is null.");
                return;
            }

            string pythonExe = settings.SearchApiPythonPathResolved;
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
        
        public static bool TryCreatePythonEnvironment(string pythonPath, string envPath = "Library/py/venv_mcp", string pythonFallback = "python3")
        {
            return CreateOrUpdatePythonEnvironment(
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

        public static bool TryCreateMcpEnvironment(string pythonPath, string envPath = "Library/py/venv_mcp", string pythonFallback = "python3")
        {
            return CreateOrUpdatePythonEnvironment(
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

        private static bool CreateOrUpdatePythonEnvironment(
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
                return false;
            }

            var pythonFile = ResolvePythonExecutable(pythonPath);
            if (!IsSimpleExecutableName(pythonFile) && !File.Exists(pythonFile))
            {
                var fallback = ResolveInterpreterFallback(requirePython310, pythonFallback);
                if (string.IsNullOrWhiteSpace(fallback))
                {
                    return false;
                }

                Debug.LogWarning($"[Indexer] Python path not found: {pythonFile}. Falling back to '{fallback}'.");
                pythonFile = fallback;
            }
            var venvFolder = ResolveVenvFolder(pythonPath, defaultVenvName);

            if (string.IsNullOrEmpty(venvFolder))
            {
                Debug.LogError("[Indexer] Invalid Python path. Expected something like Library/py/search/bin/python3, Library/py/search/Scripts/python.exe, or python3.");
                return false;
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
                        if (process == null)
                        {
                            Debug.LogError("[Indexer] Failed to start Python process for virtualenv creation.");
                            return false;
                        }

                        process.OutputDataReceived += Log;
                        process.ErrorDataReceived += LogError;

                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();
                        process.WaitForExit();

                        if (process.ExitCode != 0)
                        {
                            Debug.LogError($"[Indexer] Virtualenv creation failed with exit code {process.ExitCode}.");
                            return false;
                        }
                    }
                }

                var venvPython = ResolveVenvPython(pythonPath, venvFolder);
                if (!File.Exists(venvPython))
                {
                    Debug.LogError($"[Indexer] Virtualenv Python executable not found at: {venvPython}");
                    return false;
                }

                if (requirePython310 && !PythonSupportsMcp(venvPython))
                {
                    Debug.LogWarning("[Indexer] MCP requires Python >= 3.10. Configure MCP Python Path to a 3.10+ interpreter.");
                    EditorUtility.DisplayDialog(
                        "Python 3.10+ Required",
                        "MCP requires Python 3.10+. Please install Python 3.10+ and update the Python Path in Chat Settings.",
                        "OK");
                    return false;
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
                    if (pipProcess == null)
                    {
                        Debug.LogError("[Indexer] Failed to start pip install process.");
                        return false;
                    }

                    pipProcess.OutputDataReceived += Log;
                    pipProcess.ErrorDataReceived += LogError;

                    pipProcess.BeginOutputReadLine();
                    pipProcess.BeginErrorReadLine();
                    pipProcess.WaitForExit();

                    if (pipProcess.ExitCode != 0)
                    {
                        Debug.LogError($"[Indexer] pip install failed with exit code {pipProcess.ExitCode}.");
                        return false;
                    }
                }

                var status = shouldCreateVenv ? "created and configured" : "updated";
                Debug.Log($"[Indexer] Python environment {status} at {venvFolder}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Indexer] Failed to configure Python environment: {e.Message}");
                return false;
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
            var fallbackVenv = Path.GetFullPath(defaultVenvName);
            if (IsSimpleExecutableName(pythonPath))
            {
                return fallbackVenv;
            }

            var pythonFile = Path.GetFullPath(pythonPath);
            var normalized = pythonFile.Replace("\\", "/");
            var looksLikeVenvPython =
                normalized.EndsWith("/bin/python3", StringComparison.OrdinalIgnoreCase) ||
                normalized.EndsWith("/bin/python", StringComparison.OrdinalIgnoreCase) ||
                normalized.EndsWith("/scripts/python.exe", StringComparison.OrdinalIgnoreCase);

            if (!looksLikeVenvPython)
            {
                // Absolute system interpreters (for example D:\Python\python.exe) should still create/use the configured venv folder.
                return fallbackVenv;
            }

            var pythonDir = Directory.GetParent(pythonFile);
            return pythonDir?.Parent?.FullName ?? fallbackVenv;
        }

        private static string ResolveVenvPython(string pythonPath, string venvFolder)
        {
            if (IsSimpleExecutableName(pythonPath))
            {
                return GetVenvPythonExecutable(venvFolder);
            }
            
            var resolved = Path.GetFullPath(pythonPath);
            if (resolved.StartsWith(venvFolder, StringComparison.OrdinalIgnoreCase) && !File.Exists(resolved))
            {
                return GetVenvPythonExecutable(venvFolder);
            }

            if (resolved.StartsWith(venvFolder, StringComparison.OrdinalIgnoreCase))
            {
                return resolved;
            }

            // If pythonPath points to a global/system interpreter, use the venv interpreter for installs and checks.
            return GetVenvPythonExecutable(venvFolder);
        }

        private static string GetVenvPythonExecutable(string venvFolder)
        {
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                return Path.Combine(venvFolder, "Scripts", "python.exe");
            }

            var python3 = Path.Combine(venvFolder, "bin", "python3");
            if (File.Exists(python3))
                return python3;

            return Path.Combine(venvFolder, "bin", "python");
        }

        private static bool IsSimpleExecutableName(string pythonPath)
        {
            return pythonPath.IndexOf(Path.DirectorySeparatorChar) == -1 &&
                   pythonPath.IndexOf(Path.AltDirectorySeparatorChar) == -1;
        }

        private static string DeriveInterpreterFallback(bool requirePython310)
        {
            return "python3";
        }

        private static string ResolveInterpreterFallback(bool requirePython310, string pythonFallback)
        {
            if (!string.IsNullOrWhiteSpace(pythonFallback))
            {
                if (IsSimpleExecutableName(pythonFallback))
                    if (TestInterpreter(pythonFallback, requirePython310))
                        return pythonFallback;
                
                if (File.Exists(pythonFallback) && TestInterpreter(pythonFallback, requirePython310))
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

            if (requirePython310)
            {
                Debug.LogError("[Indexer] No Python 3.10+ interpreter found. Install Python 3.10+ and configure the Python Path.");
                EditorUtility.DisplayDialog(
                    "Python 3.10+ Not Found",
                    "No Python 3.10+ interpreter was found. Install Python 3.10+ and update the Python Path in Chat Settings.",
                    "OK");
                return null;
            }

            return DeriveInterpreterFallback(requirePython310);
        }

        private static string[] GetInterpreterCandidates(bool requirePython310)
        {
            var windowsBase = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var winPy313 = Path.Combine(windowsBase, "Programs", "Python", "Python313", "python.exe");
            var winPy312 = Path.Combine(windowsBase, "Programs", "Python", "Python312", "python.exe");
            var winPy311 = Path.Combine(windowsBase, "Programs", "Python", "Python311", "python.exe");
            var winPy310 = Path.Combine(windowsBase, "Programs", "Python", "Python310", "python.exe");

            if (requirePython310)
            {
                return new[]
                {
                    "python3",
                    "python",
                    "py",
                    "python3.13",
                    "python3.12",
                    "python3.11",
                    "python3.10",
                    "/opt/homebrew/bin/python3.13",
                    "/opt/homebrew/bin/python3.12",
                    "/opt/homebrew/bin/python3.11",
                    "/opt/homebrew/bin/python3.10",
                    "/usr/local/bin/python3.13",
                    "/usr/local/bin/python3.12",
                    "/usr/local/bin/python3.11",
                    "/usr/local/bin/python3.10",
                    "/usr/bin/python3",
                    winPy313,
                    winPy312,
                    winPy311,
                    winPy310,
                    @"C:\Python313\python.exe",
                    @"C:\Python312\python.exe",
                    @"C:\Python311\python.exe",
                    @"C:\Python310\python.exe"
                };
            }

            return new[]
            {
                "python3",
                "python",
                "/opt/homebrew/bin/python3",
                "/usr/local/bin/python3",
                winPy313,
                winPy312,
                winPy311,
                winPy310
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
