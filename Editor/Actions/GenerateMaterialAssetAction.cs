using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GPTUnity.Helpers;
using UnityEditor;
using UnityEngine;

namespace GPTUnity.Actions
{
    [GPTAction(@"Generates a material asset file.")]
    public class GenerateMaterialAssetAction : GPTActionBase
    {
        [GPTParameter("Output file name without extension")] 
        public string FileName { get; set; }

        [GPTParameter("Best location matching this file. Ex: Assets/")]
        public string PathToDirectory { get; set; } = "NewFile";
        
        [GPTParameter("Shader name (If assign shader by name)")]
        public string ShaderName { get; set; }
        
        [GPTParameter("Shader asset path to create the material with")]
        public string ShaderPath { get; set; }
        
        // [GPTParameter("Shader params to set containing a list of (key, value and type(float, range, color, vector, texture, int))", actionName: nameof(ShaderParamsSchema))]
        // public List<SerializedKeyValuePair> ShaderParams { get; set; }
        
        [GPTParameter("Shader params: Ex: _color:0.1,0.2,1,1:color;_mainTex:Assets/Textures/1.png:texture;_value:0.1:float;_intensity:2:int;_dir:0.1,0.2,1,1:vector;")]
        public string ShaderParams { get; set; }

        public override string Description => $"Created material {Highlight(FileName)}";

        // public static object ShaderParamsSchema() => new
        // {
        //     type = "array",
        //     description = "Shader params like _color, _mainTex, etc...",
        //     items = new
        //     {
        //         type = "object",
        //         properties = new
        //         {
        //             key = new { type = "string", description = "Shader property key" },
        //             value = new { type = "string", description = "Shader property value" },
        //             type = new { type = "string", description = "Shader property type (float, color, vector, texture, int)" }
        //         }
        //     }
        // };

        public override async Task<string> Execute()
        {
#if UNITY_EDITOR
            var shaderAsset = default(Shader);
            
            // 2. Load the newly created shader
            if (!string.IsNullOrEmpty(ShaderName))
            {
                shaderAsset = Shader.Find(ShaderName);
                if (!shaderAsset)
                    throw new Exception($"Could not find shader: {ShaderName}");
            }
            else if (!string.IsNullOrEmpty(ShaderPath))
            {
                if (!UnityAiHelpers.TryFindAsset(ShaderPath, typeof(Shader), out var theShader))
                {
                    throw new Exception($"Shader not found at path '{ShaderPath}'.");
                }

                shaderAsset = theShader as Shader;
            }

            // 3. Create a material
            var material = new Material(shaderAsset);

            AssignShaderParams(material);

            var matPath = GetOutputPath();
            
            // Parent directory must exist before creating asset at Assets/Materials/PlayerShip_Material
            var parentDir = System.IO.Path.GetDirectoryName(matPath);
            if (!string.IsNullOrEmpty(parentDir) && !System.IO.Directory.Exists(parentDir))
            {
                System.IO.Directory.CreateDirectory(parentDir);
            }
            
            AssetDatabase.CreateAsset(material, matPath);
            AssetDatabase.Refresh();
            
            return $"Created new material '{material.name} at {matPath}'.";
#endif
        }

        private void AssignShaderParams(Material material)
        {
            if (ShaderParams == null) 
                return;

            var shaderParamsArr = ShaderParams
                .Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(pair => pair.Split(new char[] { ':' }, StringSplitOptions.None))
                .Select(arr => new { key = arr[0], value = arr[1], type = arr[2].ToLower() });
            
            foreach (var shaderParam in shaderParamsArr)
            {
                switch (shaderParam.type)
                {
                    case "float":
                    case "range":
                        if (float.TryParse(shaderParam.value, out var floatVal))
                            material.SetFloat(shaderParam.key, floatVal);
                        else
                            Debug.LogWarning($"Could not parse float for key {shaderParam.key}: '{shaderParam.value}'");
                        break;
                    case "color":
                        if (ColorUtility.TryParseHtmlString(shaderParam.value, out var colorVal))
                            material.SetColor(shaderParam.key, colorVal);
                        else
                        {
                            var parts2 = shaderParam.value.Split(',');
                            if (parts2.Length == 4 &&
                                float.TryParse(parts2[0], out var x2) &&
                                float.TryParse(parts2[1], out var y2) &&
                                float.TryParse(parts2[2], out var z2) &&
                                float.TryParse(parts2[3], out var w2))
                            {
                                material.SetColor(shaderParam.key, new Color(x2, y2, z2, w2));
                            }
                            else
                                Debug.LogWarning($"Could not parse color for key {shaderParam.key}: '{shaderParam.value}' (expected HTML color string like #RRGGBB or #RRGGBBAA)");
                        }
                        break;
                    case "vector":
                        // Expecting value format: "x,y,z,w"
                        var parts = shaderParam.value.Split(',');
                        if (parts.Length == 4 &&
                            float.TryParse(parts[0], out var x) &&
                            float.TryParse(parts[1], out var y) &&
                            float.TryParse(parts[2], out var z) &&
                            float.TryParse(parts[3], out var w))
                        {
                            material.SetVector(shaderParam.key, new Vector4(x, y, z, w));
                        }
                        else
                        {
                            Debug.LogWarning($"Could not parse vector4 for key {shaderParam.key}: '{shaderParam.value}' (expected format: x,y,z,w)");
                        }
                        break;
                    case "texture":
                        // Value should be a path to a texture asset
                        var tex = AssetDatabase.LoadAssetAtPath<Texture>(shaderParam.value);
                        if (tex != null)
                            material.SetTexture(shaderParam.key, tex);
                        else
                            Debug.LogWarning($"Could not find texture at path: {shaderParam.value} for key {shaderParam.key}");
                        break;
                    case "int":
                        if (int.TryParse(shaderParam.value, out var intVal))
                            material.SetInt(shaderParam.key, intVal);
                        else
                            Debug.LogWarning($"Could not parse int for key {shaderParam.key}: '{shaderParam.value}'");
                        break;
                    default:
                        Debug.LogWarning($"Unknown shader property type '{shaderParam.type}' for key {shaderParam.key}");
                        break;
                }
            }
        }

        protected string GetOutputPath()
        {
            var finalPathToDirectory = PathToDirectory;
            if (!finalPathToDirectory.EndsWith("/"))
                finalPathToDirectory += "/";
            return finalPathToDirectory + FileName + ".mat";
        }
    }
}