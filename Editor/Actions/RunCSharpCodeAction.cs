using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using GPTUnity.Actions.Interfaces;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace GPTUnity.Actions
{
    [GPTAction("Runs C# code inside the Unity Editor against the project's loaded assemblies. Prefer dedicated Unity actions first, and use this as an advanced escape hatch for editor-side inspection or automation.", Name = "eval_csharp")]
    public class RunCSharpCodeAction : GPTAssistantAction, IGPTActionThatContainsCode
    {
        [GPTParameter("C# statements to execute inside an async Run() method body. You may use UnityEditor, UnityEngine, project types, Debug.Log, and optionally return a value.", true)]
        public string Code { get; set; }

        [GPTParameter("Refresh the AssetDatabase after execution if the code created or changed Unity assets.")]
        public bool RequiresAssetsRefresh { get; set; }

        public string Content => Code;

        public override async Task<string> Execute()
        {
#if UNITY_EDITOR
            if (string.IsNullOrWhiteSpace(Code))
                throw new Exception("Code is required.");

            var result = await CSharpEvalRunner.ExecuteAsync(Code);

            if (RequiresAssetsRefresh)
            {
                AssetDatabase.Refresh();
            }

            return result;
#else
            throw new Exception("eval_csharp is only available in the Unity Editor.");
#endif
        }

        public static class CSharpEvalRunner
        {
            private const string GeneratedNamespace = "GPTUnity.DynamicEval";

            internal static async Task<string> ExecuteAsync(string userCode)
            {
                var evalId = Guid.NewGuid().ToString("N");
                var typeName = $"EvalEntry_{evalId}";
                var tempDir = Path.Combine(Application.temporaryCachePath, "gpt_csharp_eval");
                Directory.CreateDirectory(tempDir);

                var sourcePath = Path.Combine(tempDir, $"{typeName}.cs");
                var assemblyPath = Path.Combine(tempDir, $"{typeName}.dll");
                var pdbPath = Path.ChangeExtension(assemblyPath, ".pdb");
                var wrapperSource = BuildWrapperSource(typeName, userCode);
                File.WriteAllText(sourcePath, wrapperSource, Encoding.UTF8);

                try
                {
                    var compilerMessages = await BuildAssemblyAsync(assemblyPath, sourcePath);
                    var errors = compilerMessages
                        .Where(message => message.type == CompilerMessageType.Error)
                        .Select(FormatCompilerMessage)
                        .ToArray();

                    if (errors.Length > 0)
                    {
                        throw new Exception("C# eval compile failed:\n" + string.Join("\n", errors));
                    }

                    var assemblyBytes = File.ReadAllBytes(assemblyPath);
                    var pdbBytes = File.Exists(pdbPath) ? File.ReadAllBytes(pdbPath) : null;
                    var assembly = pdbBytes != null
                        ? System.Reflection.Assembly.Load(assemblyBytes, pdbBytes)
                        : System.Reflection.Assembly.Load(assemblyBytes);

                    var fullTypeName = $"{GeneratedNamespace}.{typeName}";
                    var evalType = assembly.GetType(fullTypeName, throwOnError: true);
                    var runMethod = evalType.GetMethod("Run", BindingFlags.Public | BindingFlags.Static);
                    if (runMethod == null)
                        throw new Exception($"Compiled type '{fullTypeName}' did not expose a public static Run method.");

                    var capturedLogs = new List<string>();
                    void HandleLog(string condition, string stackTrace, LogType logType)
                    {
                        capturedLogs.Add($"[{logType}] {condition}");
                    }

                    Application.logMessageReceived += HandleLog;
                    try
                    {
                        var invocationResult = runMethod.Invoke(null, null);
                        if (invocationResult is not Task task)
                            throw new Exception("Compiled Run method did not return a Task.");

                        await task;

                        object returnValue = null;
                        var taskType = task.GetType();
                        if (taskType.IsGenericType && taskType.GetProperty("Result") != null)
                        {
                            returnValue = taskType.GetProperty("Result")?.GetValue(task);
                        }

                        return FormatExecutionResult(returnValue, capturedLogs);
                    }
                    catch (TargetInvocationException ex) when (ex.InnerException != null)
                    {
                        throw ex.InnerException;
                    }
                    finally
                    {
                        Application.logMessageReceived -= HandleLog;
                    }
                }
                finally
                {
                    TryDelete(sourcePath);
                    TryDelete(assemblyPath);
                    TryDelete(pdbPath);
                }
            }

            public static string BuildWrapperSource(string typeName, string userCode)
            {
                return
$@"using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace {GeneratedNamespace}
{{
    public static class {typeName}
    {{
        public static async Task<object> Run()
        {{
{IndentCode(userCode, 3)}
            return null;
        }}
    }}
}}";
            }

            public static string[] CollectReferencePaths()
            {
                return AppDomain.CurrentDomain
                    .GetAssemblies()
                    .Where(assembly => !assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
                    .Select(assembly => assembly.Location)
                    .Where(File.Exists)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }

            private static Task<CompilerMessage[]> BuildAssemblyAsync(string assemblyPath, string sourcePath)
            {
                var completion = new TaskCompletionSource<CompilerMessage[]>();
                var builder = new AssemblyBuilder(assemblyPath, new[] { sourcePath })
                {
                    flags = AssemblyBuilderFlags.EditorAssembly,
                    additionalReferences = CollectReferencePaths(),
                    buildTarget = EditorUserBuildSettings.activeBuildTarget,
                    buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup
                };

                builder.buildFinished += (_, messages) => { completion.TrySetResult(messages ?? Array.Empty<CompilerMessage>()); };

                if (!builder.Build())
                    throw new Exception("Unity AssemblyBuilder could not start the eval compilation.");

                return completion.Task;
            }

            private static string FormatExecutionResult(object returnValue, List<string> capturedLogs)
            {
                var summary = returnValue == null
                    ? "C# code executed successfully. Return value: null"
                    : $"C# code executed successfully. Return value: {returnValue}";

                if (capturedLogs == null || capturedLogs.Count == 0)
                    return summary;

                return summary + "\nCaptured logs:\n" + string.Join("\n", capturedLogs);
            }

            private static string FormatCompilerMessage(CompilerMessage message)
            {
                var location = string.IsNullOrWhiteSpace(message.file)
                    ? string.Empty
                    : $"{Path.GetFileName(message.file)}:{message.line}:{message.column}: ";

                return $"{location}{message.message}";
            }

            private static string IndentCode(string code, int indentLevel)
            {
                var indent = new string(' ', indentLevel * 4);
                var normalized = (code ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
                var lines = normalized.Split('\n');
                return string.Join("\n", lines.Select(line => indent + line));
            }

            private static void TryDelete(string path)
            {
                try
                {
                    if (File.Exists(path))
                        File.Delete(path);
                }
                catch
                {
                    // Temporary eval artifacts are best-effort cleanup.
                }
            }
        }
    }
}
