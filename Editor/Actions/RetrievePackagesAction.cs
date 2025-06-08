using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor.PackageManager.UI;
using UnityEngine;

namespace GPTUnity.Actions
{
    [GPTAction("Retrieves all installed Unity packages")]
    public class RetrieveInstalledPackagesAction : GPTAssistantAction
    {
        private string _result = "";

        public override async Task<string> Execute()
        {
#if UNITY_EDITOR

            await RetrieveFromClientList();

            // RetrieveFromManifest();

            return _result;
#endif
        }

        private async Task RetrieveFromClientList()
        { 
            var packages = UnityEditor.PackageManager.Client.List();
            var sb = new StringBuilder();
            sb.AppendLine("Installed packages:");
            
            // ListRequest is not enumerable; wait for completion and then access .Result
            while (!packages.IsCompleted)
            {
                await Task.Delay(100);
            }
            
            if (packages.Status == UnityEditor.PackageManager.StatusCode.Success)
            {
                foreach (var package in packages.Result)
                {
                    sb.AppendLine($"- {package.name}: {package.version}");
                }
            }
            else
            {
                sb.AppendLine("Failed to retrieve packages.");
            }
            
            _result = sb.ToString();
        }

        // private void RetrieveFromManifest()
        // {
        //     var manifestPath = Path.Combine(Application.dataPath, "../Packages/manifest.json");
        //     if (!File.Exists(manifestPath))
        //     {
        //         throw new Exception("manifest.json not found.");
        //     }
        //     
        //     var json = File.ReadAllText(manifestPath);
        //     var jp = Newtonsoft.Json.Linq.JObject.Parse(json);
        //     var dependencies = jp["dependencies"] as Newtonsoft.Json.Linq.JObject;
        //     if (dependencies == null)
        //     {
        //         dependencies = new Newtonsoft.Json.Linq.JObject();
        //         jp["dependencies"] = dependencies;
        //     }
        //
        //     BuildContent(dependencies);
        //     BuildDescription(dependencies);
        // }
        //
        // private void BuildDescription(JObject dependencies)
        // {
        //     var sb = new StringBuilder();
        //     sb.AppendLine("Installed packages:");
        //     foreach (var package in dependencies)
        //     {
        //         sb.AppendLine($"- {Highlight(package.Key)}: {Highlight(package.Value.ToString())}");
        //     }   
        //     _description = sb.ToString();
        // }
        //
        // private void BuildContent(JObject dependencies)
        // {
        //     var sb = new StringBuilder();
        //     sb.AppendLine("Installed packages:");
        //     foreach (var package in dependencies)
        //     {
        //         sb.AppendLine($"- {package.Key}: {package.Value}");
        //     }   
        //     _result = sb.ToString();
        // }
    }
}
