namespace GPTUnity.Actions
{
    [GPTAction("Creates a shader source file from provided code. Use this for advanced Unity shader authoring.", Name = "create_shader_file")]
    public class GenerateShaderAction : CreateFileActionBase
    {
        [GPTParameter("Shader source code content.", true, Name = "shader_code")] public string ShaderCode { get; set; }

        public override string Content => ShaderCode;
        protected override string DefaultFileExtension => ".shader";
        protected override string DefaultDirectory => "Assets/Shaders/";
    }
    
    [GPTAction("Creates a plain text file in the Unity project. Use this for notes, prompts, data samples, or support artifacts.", Name = "create_text_file")]
    public class GenerateTextAction : CreateFileActionBase
    {
        [GPTParameter("Full text content to write into the file.", true, Name = "text_content")] 
        public string TextContent { get; set; }
        
        public override string Content => TextContent;
        protected override string DefaultFileExtension => ".txt";
        protected override string DefaultDirectory => "Assets/";
    }
    
    [GPTAction("Creates a JSON file in the Unity project after validating the provided JSON text.", Name = "create_json_file")]
    public class GenerateJsonAction : CreateFileActionBase
    {
        [GPTParameter("Valid JSON content to write into the file.", true, Name = "json_content")] 
        public string JsonContent { get; set; }
        
        public override string Content => JsonContent;
        protected override string DefaultFileExtension => ".json";
        protected override string DefaultDirectory => "Assets/";

        public override async System.Threading.Tasks.Task<string> Execute()
        {
            Newtonsoft.Json.Linq.JToken.Parse(JsonContent);
            return await base.Execute();
        }
    }
    
    [GPTAction("Generates a markdown file.", Expose = false)]
    public class GenerateMarkdownAction : CreateFileActionBase
    {
        [GPTParameter("Markdown file content")] 
        public string MarkdownContent { get; set; }
        
        public override string Content => MarkdownContent;
        protected override string DefaultFileExtension => ".md";
        protected override string DefaultDirectory => "Assets/";
    }
}
