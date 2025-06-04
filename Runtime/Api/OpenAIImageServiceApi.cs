using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace GPTUnity.Api
{
    public class OpenAIImageServiceApi : IImageServiceApi
    {
        private readonly string _key;
        
        public OpenAIImageServiceApi(string key)
        {
            _key = key;
        }
        
        public async Task<Texture2D> GenerateImage(string prompt, string model = "gpt-image-1", bool transparent = true, int width = 1024, int height = 1024, int quality = 0)
        {
            var client = new HttpClient();
            var url = "https://api.openai.com/v1/images/generations";
            var requestBody = new
            {
                model,
                prompt,
                n = 1,
                size = $"{width}x{height}",
                quality = new [] {"low", "medium", "high"}[quality],
                background = transparent ? "transparent" : "opaque",
                output_format = "png"
            };
            
            var requestJson = JsonConvert.SerializeObject(requestBody);
            
            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_key}");

            var response = await client.PostAsync(url, content);
            var responseJson = await response.Content.ReadAsStringAsync();
            
            if (responseJson.Contains("\"error\""))
            {
                throw new Exception(responseJson);
            }
            
            var result = JsonConvert.DeserializeObject<GPTImagesResponse>(responseJson);
            if (result.data == null || result.data.Length == 0)
            {
                throw new Exception("No images generated.");
            }
            
            // Create an image from the result
            var imageData = result.data[0].b64_json;
            var imageBytes = Convert.FromBase64String(imageData);
            var image = new UnityEngine.Texture2D(2, 2);
            image.LoadImage(imageBytes);

            return image;
        }
        
        class GPTImagesResponse
        {
            public GPTImageResponse[] data;
        }
        
        class GPTImageResponse
        {
            public string b64_json;
        }
    }
}