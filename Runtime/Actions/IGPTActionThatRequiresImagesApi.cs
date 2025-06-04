using GPTUnity.Api;

namespace GPTUnity.Actions.Interfaces
{
    public interface IGPTActionThatRequiresImagesApi
    {
        IImageServiceApi Images { get; set; }
    }
}