using System.Collections.Generic;
using System.Threading.Tasks;
using GPTUnity.Data;
using UnityEngine;

namespace GPTUnity.Api
{
    public interface IImageServiceApi
    {
        Task<Texture2D> GenerateImage(string prompt, string model, bool transparent, int width, int height, int quality);
    }
}