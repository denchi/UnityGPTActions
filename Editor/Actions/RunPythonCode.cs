using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using GPTUnity.Actions.Interfaces;
using UnityEngine;

namespace GPTUnity.Actions
{
    [GPTAction("Runs Python code inside Unity using unity-python.")]
    [GPTRequiresPackage("com.unity.scripting.python")]
    public class RunPythonCodeAction : GPTAssistantAction, IGPTActionThatContainsCode
    {
        [GPTParameter("The Python code to execute")]
        public string Code { get; set; }
        
        [GPTParameter("The Python Requirements If Any (example: numpy,pillow)")]
        public string Requirements { get; set; }
        
        [GPTParameter("Does require asset refresh because new assets were added?")]
        public bool RequiresAssetsRefresh { get; set; }
        
        public string Content => Code;

        public override async Task<string> Execute()
        {
#if UNITY_EDITOR
            // Install requirements if specified
            if (!string.IsNullOrWhiteSpace(Requirements))
            {
                var pkgs = Requirements.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var pkg in pkgs)
                {
                    TryInstallRequirement(pkg);
                }
            }

            var result = TryRunPythonCode(Code);

            if (RequiresAssetsRefresh)
            {
                UnityEditor.AssetDatabase.Refresh();
            }

            return result;
#endif
        }

        private string TryRunPythonCode(string code)
        {
            var type = Type.GetType("UnityEditor.Scripting.Python.PythonRunner, Unity.Scripting.Python.Editor");
            if (type != null)
            {
                var method = type.GetMethod("RunString", BindingFlags.Static | BindingFlags.Public);
                if (method != null)
                {
                    method.Invoke(null, new object[] { code, null });
                    return "Code executed successfully (Unity PythonRunner).";
                }
            }

            var fallbackResult = TryRunWithExternalPython(code);
            return $"Code executed successfully (external python). Output:\n{fallbackResult}";
        }

        private void TryInstallRequirement(string packageName)
        {
            var type = Type.GetType("UnityEditor.Scripting.Python.PythonRunner, Unity.Scripting.Python.Editor");
            if (type != null)
            {
                var method = type.GetMethod("RunString", BindingFlags.Static | BindingFlags.Public);
                if (method != null)
                {
                    method.Invoke(null, new object[] { $"import pip; pip.main(['install', '{packageName}'])", null });
                    return;
                }
            }

            // External python fallback
            var command = $"-m pip install {packageName}";
            if (RunExternalProcess("python3", command, 120000, ignoreFailures: true) != null)
                return;

            if (RunExternalProcess("python", command, 120000, ignoreFailures: true) != null)
                return;

            throw new Exception(
                $"Could not install python package '{packageName}'. PythonRunner and external python were unavailable.");
        }

        private string TryRunWithExternalPython(string code)
        {
            var tempFile = Path.Combine(Application.temporaryCachePath, $"gpt_python_{Guid.NewGuid():N}.py");
            File.WriteAllText(tempFile, code ?? string.Empty);

            try
            {
                var args = $"\"{tempFile}\"";
                var output = RunExternalProcess("python3", args, 120000, ignoreFailures: true);
                if (output != null)
                    return output;

                output = RunExternalProcess("python", args, 120000, ignoreFailures: true);
                if (output != null)
                    return output;
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }

            throw new Exception("PythonRunner not available and no external python executable ('python3'/'python') could run the script.");
        }

        private string RunExternalProcess(string executable, string arguments, int timeoutMs, bool ignoreFailures)
        {
            try
            {
                using (var process = new Process())
                {
                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = executable,
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    process.Start();

                    var stdOut = process.StandardOutput.ReadToEnd();
                    var stdErr = process.StandardError.ReadToEnd();

                    if (!process.WaitForExit(timeoutMs))
                    {
                        process.Kill();
                        throw new Exception($"Process '{executable}' timed out.");
                    }

                    if (process.ExitCode != 0)
                    {
                        if (ignoreFailures)
                            return null;

                        throw new Exception($"Process '{executable}' failed with code {process.ExitCode}: {stdErr}");
                    }

                    if (!string.IsNullOrWhiteSpace(stdErr) && !ignoreFailures)
                        return $"{stdOut}\n{stdErr}".Trim();

                    return string.IsNullOrWhiteSpace(stdOut) ? "(no output)" : stdOut.Trim();
                }
            }
            catch
            {
                if (ignoreFailures)
                    return null;

                throw;
            }
        }
    }
}
