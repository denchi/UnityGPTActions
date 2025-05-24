namespace GPTUnity.Actions
{
    [GPTAction(@"Generates an SVG image file. Requires Vector Graphics 2.0.0+ installed")]
    public class GenerateSvgImageAction : CreateFileActionBase
    {
        [GPTParameter("The contents of the SVG file")]
        public string SvgContent { get; set; }

        [GPTParameter("Description of how the SVG image should look")]
        public string ImageDescription { get; set; }

        public override string Content => SvgContent;
    }
}
