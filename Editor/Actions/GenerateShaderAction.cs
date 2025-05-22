namespace GPTUnity.Actions
{
    [GPTAction("Generates an HLSL or ShaderGraph shader file.")]
    public class GenerateShaderAction : CreateFileActionBase
    {
        [GPTParameter("Shader code content")] public string ShaderCode { get; set; }

        public override string Content => ShaderCode;
    }
}