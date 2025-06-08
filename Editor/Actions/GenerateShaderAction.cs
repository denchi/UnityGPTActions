namespace GPTUnity.Actions
{
    [GPTAction("Generates an HLSL or ShaderGraph shader file.")]
    public class GenerateShaderAction : CreateFileActionBase
    {
        [GPTParameter("Shader code content")] public string ShaderCode { get; set; }

        public override string Content => ShaderCode;
    }
    
    [GPTAction("Generates an text file.")]
    public class GenerateTextAction : CreateFileActionBase
    {
        [GPTParameter("Text file content")] 
        public string TextContent { get; set; }
        
        public override string Content => TextContent;
    }
    
    [GPTAction("Generates a json file.")]
    public class GenerateJsonAction : CreateFileActionBase
    {
        [GPTParameter("Json file content")] 
        public string JsonContent { get; set; }
        
        public override string Content => JsonContent;
    }
    
    [GPTAction("Generates a markdown file.")]
    public class GenerateMarkdownAction : CreateFileActionBase
    {
        [GPTParameter("Markdown file content")] 
        public string MarkdownContent { get; set; }
        
        public override string Content => MarkdownContent;
    }
}