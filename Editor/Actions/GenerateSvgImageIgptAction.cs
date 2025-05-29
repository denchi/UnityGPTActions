namespace GPTUnity.Actions
{
    [GPTAction(@"Generates an SVG image file")]
    [GPTRequiresPackage("com.unity.vectorgraphics")]
    public class GenerateSvgImageAction : CreateFileActionBase
    {
        [GPTParameter("The contents of the SVG file")]
        public string SvgContent { get; set; }

        [GPTParameter("Description of how the SVG image should look")]
        public string ImageDescription { get; set; }

        public override string Content => SvgContent;
    }
}
