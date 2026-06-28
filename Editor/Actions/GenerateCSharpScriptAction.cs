namespace GPTUnity.Actions
{
    [GPTAction("Creates a plain C# script asset from provided source code. Use this for editor scripts, interfaces, utilities, or non-MonoBehaviour runtime types.", Name = "create_csharp_script")]
    public class GenerateCSharpScriptAction : CreateFileActionBase
    {
        [GPTParameter("Complete C# source code to write into the script asset.", true, Name = "script_code")]
        public string ScriptCode { get; set; }

        public override string Content => ScriptCode;
        protected override string DefaultFileExtension => ".cs";
        protected override string DefaultDirectory => "Assets/Scripts/";
    }
}
