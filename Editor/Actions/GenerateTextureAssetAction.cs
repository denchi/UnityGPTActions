using System;
using System.IO;
using System.Threading.Tasks;
using GPTUnity.Actions.Interfaces;
using GPTUnity.Api;
using GPTUnity.Helpers;
using UnityEditor;
using UnityEngine;

namespace GPTUnity.Actions
{
    [GPTAction(@"Generates a texture or sprite asset file from a text prompt using the configured image API.", Name = "generate_texture_asset")]
    public class GenerateTextureAssetAction : GPTAssistantAction, IGPTActionThatRequiresReload, IGPTActionThatRequiresImagesApi, IGPTActionThatContainsCode
    {
        [GPTParameter("Output file name without extension.", true, Name = "file_name")]
        public string FileName { get; set; }

        [GPTParameter("Project-relative output folder for the generated image asset.", Name = "output_directory")]
        public string PathToDirectory { get; set; } = "Assets/Textures/";

        [GPTParameter("Prompt describing the texture or sprite to generate.", true, Name = "prompt")]
        public string Prompt { get; set; }

        [GPTParameter("Whether the generated image should use a transparent background.", Name = "transparent")]
        public bool Transparent { get; set; } = false;

        [GPTParameter("Requested output width in pixels.", Name = "width")]
        public int Width { get; set; } = 1024;

        [GPTParameter("Requested output height in pixels.", Name = "height")]
        public int Height { get; set; } = 1024;
        
        [GPTParameter("Import the generated asset as a Sprite for SpriteRenderer or UI Image usage.", Name = "save_as_sprite")]
        public bool SaveAsSprite { get; set; }
        
        public string Content => $"{Prompt} --width {Width} --height {Height} --transparent {Transparent}";

        public IImageServiceApi Images { get; set; }

        public override async Task<string> Execute()
        {
#if UNITY_EDITOR
            if (Images == null)
                throw new Exception("GPT Service API not found.");

            int sentWidth;
            int sentHeight; 
            // available sizes: 1024x1024, 1536x1024, 1024x1536
            // if Width or Height are not set, like in the available sizes
            // use closest available
            if (Width == 1536 && Height == 1024)
            {
                sentWidth = 1536;
                sentHeight = 1024;
            }
            else if (Width == 1024 && Height == 1536)
            {
                sentWidth = 1024;
                sentHeight = 1536;
            }
            else
            {
                // use default size
                sentWidth = 1024;
                sentHeight = 1024;
            }

            var tex = await Images.GenerateImage(Prompt, "gpt-image-1", Transparent, sentWidth, sentHeight, quality: 0);
            if (tex == null)
                throw new Exception("Failed to generate texture.");
            
            if (sentWidth != Width || sentHeight != Height)
            {
                var ar = Width / (float)Height;
                var sentAr = sentWidth / (float)sentHeight;

                if (Mathf.Approximately(ar, sentAr))
                {
                    var newTex = ResizeTexture(tex, Width, Height);
                    UnityEngine.Object.DestroyImmediate(tex);
                    tex = newTex;
                }
                else
                {
                    var newTex = ResizeTexture(tex, Width, Height);
                    UnityEngine.Object.DestroyImmediate(tex);
                    tex = newTex;
                    
                    // for now we will not crop the texture
                    
                    // // resize and crop
                    // var resizedTex = new Texture2D(sentWidth, sentHeight, tex.format, tex.mipmapCount > 1);
                    // Graphics.ConvertTexture(tex, resizedTex);
                    //
                    // // Crop the texture to fit the aspect ratio
                    // var cropWidth = Mathf.Min(sentWidth, resizedTex.width);
                    // var cropHeight = Mathf.Min(sentHeight, resizedTex.height);
                    // var croppedTex = new Texture2D(cropWidth, cropHeight, resizedTex.format, resizedTex.mipmapCount > 1);
                    //
                    // Graphics.CopyTexture(resizedTex, 0, 0, 0, 0, cropWidth, cropHeight, croppedTex, 0, 0, 0, 0);
                    // UnityEngine.Object.DestroyImmediate(resizedTex);    
                    // UnityEngine.Object.DestroyImmediate(tex);
                    //
                    // tex = croppedTex;
                }
            }

            var texPath = GetOutputPath();

            // Ensure directory exists
            var parentDir = Path.GetDirectoryName(texPath);
            if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
                Directory.CreateDirectory(parentDir);

            // Encode to PNG
            var pngData = tex.EncodeToPNG();
            if (pngData == null)
                throw new Exception("Failed to encode texture to PNG.");

            File.WriteAllBytes(texPath, pngData);
            AssetDatabase.ImportAsset(texPath);

            if (SaveAsSprite || Transparent)
            {
                var textureImporter = AssetImporter.GetAtPath(texPath) as TextureImporter;
                if (!textureImporter)
                    throw new Exception($"Failed to get TextureImporter for {texPath}");
                textureImporter.alphaIsTransparency = Transparent;
                textureImporter.alphaSource = TextureImporterAlphaSource.FromInput;
                
                if (SaveAsSprite)
                {
                    textureImporter.textureType = TextureImporterType.Sprite;
                    textureImporter.spriteImportMode = SpriteImportMode.Single;
                    
                    textureImporter.spritePixelsPerUnit = 100f; // adjust as necessary
                    textureImporter.spritePivot = new Vector2(0.5f, 0.5f); // pivot at center
                    
                    // textureImporter.spritesheet = null; // clear existing slices
                }
                else
                {
                    textureImporter.textureType = TextureImporterType.Default;
                }
                
                textureImporter.SaveAndReimport();
            }
            
            return $"Created new {(SaveAsSprite ? "sprite" : "texture")} at {texPath}";
#else
            return "Editor only";
#endif
            Texture2D ResizeTexture(Texture2D source, int newWidth, int newHeight)
            {
                RenderTexture rt = new RenderTexture(newWidth, newHeight, 0);
                RenderTexture currentRT = RenderTexture.active;

                // Copy the source texture to the render texture
                Graphics.Blit(source, rt);
                RenderTexture.active = rt;

                // Read the pixels from the RenderTexture into a new Texture2D
                Texture2D newTex = new Texture2D(newWidth, newHeight, source.format, false);
                newTex.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
                newTex.Apply();

                // Clean up
                RenderTexture.active = currentRT;
                rt.Release();

                return newTex;
            }
        }

        protected string GetOutputPath()
        {
            var finalPathToDirectory = PathToDirectory;
            if (!finalPathToDirectory.EndsWith("/"))
                finalPathToDirectory += "/";
            // Ensure path is relative to Assets
            if (!finalPathToDirectory.StartsWith("Assets/"))
                finalPathToDirectory = "Assets/" + finalPathToDirectory;
            return finalPathToDirectory + FileName + ".png";
        }

    }
}
