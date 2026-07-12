using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor.PackageManager;

namespace GPTUnity.Actions
{
    [GPTAction("Runs Unity EditMode or PlayMode tests. If the Unity Test Framework package is not installed, returns a clear missing-package message.", Name = "run_unity_tests")]
    public class RunUnityTestsAction : GPTAssistantAction
    {
        private const string TestFrameworkPackageName = "com.unity.test-framework";
        private const string RunnerBridgeTypeName = "GPTUnity.Actions.TestRunner.UnityTestRunnerBridge, DeathByGravity.GPTActions.Editor.TestRunner";

        [GPTParameter("Test mode: EditMode or PlayMode.")]
        public string Mode { get; set; } = "EditMode";

        [GPTParameter("Optional exact test names to run.")]
        public string[] TestNames { get; set; }

        [GPTParameter("Optional assembly names to limit the run.")]
        public string[] AssemblyNames { get; set; }

        [GPTParameter("Optional category names to include.")]
        public string[] CategoryNames { get; set; }

        [GPTParameter("Optional group or regex names to include.")]
        public string[] GroupNames { get; set; }

        [GPTParameter("Include passed tests in the output.")]
        public bool IncludePassedTests { get; set; }

        public override async Task<string> Execute()
        {
#if UNITY_EDITOR
            if (!await IsPackageInstalled(TestFrameworkPackageName))
                return $"No package installed: {TestFrameworkPackageName}";

            var bridgeType = Type.GetType(RunnerBridgeTypeName);
            if (bridgeType == null)
                return $"Unity Test Runner is installed, but the test bridge is unavailable for package {TestFrameworkPackageName}.";

            var runMethod = bridgeType.GetMethod("RunTestsAsync", BindingFlags.Public | BindingFlags.Static);
            if (runMethod == null)
                return "Unity Test Runner bridge is missing the RunTestsAsync entry point.";

            var task = runMethod.Invoke(null, new object[]
            {
                Mode,
                TestNames,
                AssemblyNames,
                CategoryNames,
                GroupNames,
                IncludePassedTests
            }) as Task<string>;

            if (task == null)
                return "Unity Test Runner bridge did not return a valid task.";

            return await task;
#else
            return "This action can only be run in the Unity Editor.";
#endif
        }

#if UNITY_EDITOR
        private static async Task<bool> IsPackageInstalled(string packageName)
        {
            var request = Client.List(true, false);
            while (!request.IsCompleted)
            {
                await Task.Delay(100);
            }

            if (request.Status != StatusCode.Success || request.Result == null)
                return false;

            foreach (var package in request.Result)
            {
                if (string.Equals(package.name, packageName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
#endif
    }
}
