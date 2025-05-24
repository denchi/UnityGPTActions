using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace GPTUnity.Actions
{
    [GPTAction("Retrieves all installed Unity packages from manifest.json.")]
    public class RetrievePackagesAction : GPTActionBase
    {
        public override string Description => _description;

        private string _result = "";
        private string _description = "";

        public override async Task<string> Execute()
        {
            #if UNITY_EDITOR
            
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

            BuildContent(dependencies);
            BuildDescription(dependencies);

            return _result;

#endif
        }

        private void BuildDescription(JObject dependencies)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Installed packages:");
            foreach (var package in dependencies)
            {
                sb.AppendLine($"- {Highlight(package.Key)}: {Highlight(package.Value.ToString())}");
            }   
            _description = sb.ToString();
        }

        private void BuildContent(JObject dependencies)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Installed packages:");
            foreach (var package in dependencies)
            {
                sb.AppendLine($"- {package.Key}: {package.Value}");
            }   
            _result = sb.ToString();
        }
    }
}
