using System.Collections.Generic;
using System.Reflection;

namespace GPTUnity.Actions
{
    [GPTAction]
    public class CreateCustomShaderAction : CreateFileActionBase
    {
        [GPTParameter("Custom shader code to use")]
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
    }
}