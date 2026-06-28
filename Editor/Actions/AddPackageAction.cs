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
    [GPTAction("Adds a Unity package dependency to the project manifest.", Name = "add_package")]
    public class AddPackageAction : GPTAssistantAction, IGPTActionThatRequiresReload
    {
        [GPTParameter("Package name to add, for example 'com.unity.textmeshpro'.", required: true, Name = "package_name")]
        public string PackageName { get; set; }

        [GPTParameter("Package version to add, for example '3.0.6'.", required: true, Name = "version")]
        public string Version { get; set; }

        public override async Task<string> Execute()
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(PackageName))
            {
                throw new Exception("Package name and version cannot be empty.");
            }

            string packageId = string.IsNullOrEmpty(Version)
                ? PackageName
                : $"{PackageName}@{Version}";
            
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
