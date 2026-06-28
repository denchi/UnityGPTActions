namespace GPTUnity.Actions
{
    [GPTAction("Creates a custom Unity shader asset from provided shader source code. Use this for advanced shader authoring when a generated material is not enough.", Name = "create_custom_shader")]
    public class CreateCustomShaderAction : CreateFileActionBase
    {
        [GPTParameter("Full Unity shader source code to write into the .shader asset.", true, Name = "shader_code")]
        public string ShaderCode { get; set; } = @"
            Shader ""Custom/Shader"" {
                Properties {
                    _Color (""Main Color"", Color) = (1,1,1,1)
                }
                SubShader {
                    Pass {
                        Color [_Color]
                    }
                }
            }";

        public override string Content => ShaderCode;
        protected override string DefaultFileExtension => ".shader";
        protected override string DefaultDirectory => "Assets/Shaders/";
    }
}
