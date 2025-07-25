using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using GPTUnity.Actions.Interfaces;
using Newtonsoft.Json;
using UnityEngine;

namespace GPTUnity.Actions
{
    [GPTAction("Removes a Unity package from manifest.json.")]
    public class RemovePackageAction : GPTAssistantAction, IGPTActionThatRequiresReload
    {
        [GPTParameter("Name of the package to remove, e.g. com.unity.textmeshpro")]
        public string PackageName { get; set; }

        public override async Task<string> Execute()
        {
            #if UNITY_EDITOR
            
            if (string.IsNullOrEmpty(PackageName))
            {
                throw new Exception("Package name and version cannot be empty.");
            }
            
            var manifestPath = Path.Combine(Application.dataPath, "../Packages/manifest.json");
            if (!File.Exists(manifestPath))
            {
                throw new Exception("manifest.json not found.");
            }
            
            var json = File.ReadAllText(manifestPath);
            var jp = Newtonsoft.Json.Linq.JObject.Parse(json);
            var dependencies = jp["dependencies"] as Newtonsoft.Json.Linq.JObject;
            if (dependencies == null)
            {
                dependencies = new Newtonsoft.Json.Linq.JObject();
                jp["dependencies"] = dependencies;
            }
            
            dependencies.Remove(PackageName);
            var newJson = jp.ToString(Formatting.Indented);
            File.WriteAllText(manifestPath, newJson);
            
            return $"Removed package '{PackageName}' from manifest.json.";
            
            #endif
        }
    }
}
