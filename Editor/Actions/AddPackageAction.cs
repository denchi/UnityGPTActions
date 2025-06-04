using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using GPTUnity.Actions.Interfaces;
using Newtonsoft.Json;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
#endif

namespace GPTUnity.Actions
{
    [GPTAction("Adds a Unity package to manifest.json.")]
    public class AddPackageAction : GPTAssistantAction, IGPTActionThatRequiresReload
    {
        [GPTParameter("Name of the package to add, e.g. com.unity.textmeshpro", required: true)]
        public string PackageName { get; set; }

        [GPTParameter("Version of the package to add, e.g. 3.0.6", required: true)]
        public string Version { get; set; }

        public override string Description => $"Added package: {Highlight(PackageName + "@" + Version)}";

        public override async Task<string> Execute()
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(PackageName))
            {
                throw new Exception("Package name and version cannot be empty.");
            }

            string packageId = $"{PackageName}@{Version}";
            
            if (!string.IsNullOrEmpty(Version))
            {
                packageId = $"{packageId}@{Version}";
            }
            
            AddRequest request = Client.Add(packageId);

            // Wait for the request to complete
            while (!request.IsCompleted)
            {
                await Task.Delay(100);
            }

            if (request.Status == StatusCode.Failure)
            {
                throw new Exception($"Failed to add package: {request.Error.message}");
            }

            return $"Added package: {packageId}";
#endif
        }
    }
}
