namespace GPTUnity.Actions
{
    [GPTAction(@"Generates a mesh as an wavefront obj file")]
    public class GenerateMeshAssetAction : CreateFileActionBase
    {
        [GPTParameter("The contents of the Wavefront .obj file")]
        public string Asset { get; set; }

        [GPTParameter("Description of how the asset should look")]
        public string AssetDescription { get; set; }

        public override string Content => Asset;
    }
}