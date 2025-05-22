namespace GPTUnity.Actions
{
    [GPTAction("Generates a C# script with a given description and content.")]
    public class GenerateCSharpScriptAction : CreateFileActionBase
    {
        [GPTParameter("Full script code content")]
        public string ScriptCode { get; set; }

        public override string Content => ScriptCode;
    }
}