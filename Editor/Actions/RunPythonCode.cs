using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace GPTUnity.Actions
{
    [GPTAction("Runs Python code inside Unity using unity-python.")]
    [GPTRequiresPackage("com.unity.scripting.python")]
    public class RunPythonCodeAction : GPTActionBase, IActionThatContainsCode
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
                    TryRunPythonCode($"import pip; pip.main(['install', '{pkg}'])");
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
            if (type == null)
            {
                throw new Exception("PythonRunner not available. Make sure 'com.unity.scripting.python' is installed.");
            }

            var method = type.GetMethod("RunString", BindingFlags.Static | BindingFlags.Public);
            if (method == null)
            {
                throw new Exception("PythonRunner.RunString method not found.");
            }
            
            // Pass both parameters: code and null for scopeName
            method.Invoke(null, new object[] { code, null });
            
            return "Code executed successfully!";
        }
    }
}
