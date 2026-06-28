using System;
using System.Text.RegularExpressions;

namespace GPTUnity.Actions
{
    [GPTAction("Creates a Unity MonoBehaviour C# script asset from provided source code. Prefer this when authoring gameplay or component scripts.", Name = "create_monobehaviour_script")]
    public class CreateMonoBehaviourAction : CreateFileActionBase
    {
        [GPTParameter("Complete C# script contents for the MonoBehaviour.", true, Name = "script_code")]
        public string ScriptCode { get; set; }

        public override string Content => ScriptCode;
        protected override string DefaultFileExtension => ".cs";
        protected override string DefaultDirectory => "Assets/Scripts/";

        public override async System.Threading.Tasks.Task<string> Execute()
        {
            ValidateScriptCode();
            return await base.Execute();
        }

        private void ValidateScriptCode()
        {
            if (string.IsNullOrWhiteSpace(ScriptCode))
                throw new Exception("ScriptCode is required.");

            if (string.IsNullOrWhiteSpace(FileName))
                throw new Exception("FileName is required.");

            var classMatch = Regex.Match(ScriptCode, @"class\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)");
            if (classMatch.Success && !string.Equals(classMatch.Groups["name"].Value, FileName, StringComparison.Ordinal))
            {
                throw new Exception($"MonoBehaviour class name '{classMatch.Groups["name"].Value}' must match FileName '{FileName}'.");
            }
        }
    }
}
