using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace GPTUnity.Actions
{
    [GPTAction("Adds a Unity package to manifest.json.")]
    public class AddPackageAction : GPTActionBase, IActionThatRequiresReload
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
            
            if (string.IsNullOrEmpty(Version))
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
            
            dependencies[PackageName] = Version;
            var newJson = jp.ToString(Formatting.Indented);
            File.WriteAllText(manifestPath, newJson);

            return $"Added package: {PackageName + "@" + Version}";

#endif
        }
    }
}
