using System;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace GPTUnity.Actions.TestRunner
{
    public sealed class UnityTestRunnerBridge : ScriptableObject, ICallbacks
    {
        private TaskCompletionSource<string> _completionSource;
        private StringBuilder _output;
        private bool _includePassedTests;
        private TestRunnerApi _api;

        public static Task<string> RunTestsAsync(
            string mode,
            string[] testNames,
            string[] assemblyNames,
            string[] categoryNames,
            string[] groupNames,
            bool includePassedTests)
        {
            var bridge = CreateInstance<UnityTestRunnerBridge>();
            bridge.hideFlags = HideFlags.HideAndDontSave;
            bridge._completionSource = new TaskCompletionSource<string>();
            bridge._includePassedTests = includePassedTests;
            bridge._output = new StringBuilder();

            bridge._api = ScriptableObject.CreateInstance<TestRunnerApi>();
            bridge._api.hideFlags = HideFlags.HideAndDontSave;
            bridge._api.RegisterCallbacks(bridge);

            var filter = new Filter
            {
                testMode = ParseMode(mode),
                testNames = Normalize(testNames),
                assemblyNames = Normalize(assemblyNames),
                categoryNames = Normalize(categoryNames),
                groupNames = Normalize(groupNames)
            };

            bridge._output.AppendLine($"Running {filter.testMode} tests...");
            bridge._api.Execute(new ExecutionSettings(filter));
            return bridge._completionSource.Task;
        }

        public void RunStarted(ITestAdaptor testsToRun)
        {
        }

        public void RunFinished(ITestResultAdaptor result)
        {
            _output.AppendLine($"Result: {result.ResultState}");
            _output.AppendLine($"Passed: {result.PassCount}");
            _output.AppendLine($"Failed: {result.FailCount}");
            _output.AppendLine($"Skipped: {result.SkipCount}");

            _completionSource.TrySetResult(_output.ToString().Trim());
            if (_api != null)
            {
                _api.UnregisterCallbacks(this);
                DestroyImmediate(_api);
                _api = null;
            }
            DestroyImmediate(this);
        }

        public void TestStarted(ITestAdaptor test)
        {
        }

        public void TestFinished(ITestResultAdaptor result)
        {
            if (result.Test == null || result.Test.IsSuite)
                return;

            if (string.Equals(result.ResultState, "Passed", StringComparison.OrdinalIgnoreCase))
            {
                if (_includePassedTests)
                    _output.AppendLine($"PASS {result.Name}");

                return;
            }

            if (string.Equals(result.ResultState, "Failed", StringComparison.OrdinalIgnoreCase))
            {
                _output.AppendLine($"FAIL {result.Name}");
                AppendIfPresent(result.Message);
                AppendIfPresent(result.StackTrace);
                return;
            }

            _output.AppendLine($"{result.ResultState?.ToUpperInvariant() ?? "UNKNOWN"} {result.Name}");
            AppendIfPresent(result.Message);
        }

        private void AppendIfPresent(string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                _output.AppendLine(value.Trim());
        }

        private static TestMode ParseMode(string mode)
        {
            return string.Equals(mode, "PlayMode", StringComparison.OrdinalIgnoreCase)
                ? TestMode.PlayMode
                : TestMode.EditMode;
        }

        private static string[] Normalize(string[] values)
        {
            if (values == null || values.Length == 0)
                return null;

            var filtered = Array.FindAll(values, value => !string.IsNullOrWhiteSpace(value));
            return filtered.Length == 0 ? null : filtered;
        }
    }
}
