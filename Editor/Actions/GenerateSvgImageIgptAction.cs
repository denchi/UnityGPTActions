namespace GPTUnity.Actions
{
    [GPTAction(@"Creates an SVG asset file from provided SVG markup. Requires the Vector Graphics package when importing or rendering the asset in Unity.", Name = "create_svg_asset")]
    [GPTRequiresPackage("com.unity.vectorgraphics")]
    public class GenerateSvgImageAction : CreateFileActionBase
    {
        [GPTParameter("Raw SVG markup to write into the asset file.", true, Name = "svg_content")]
        public string SvgContent { get; set; }

        [GPTParameter("Optional human-readable description of the intended SVG asset.", Name = "image_description")]
        public string ImageDescription { get; set; }

        public override string Content => SvgContent;
        protected override string DefaultFileExtension => ".svg";
        protected override string DefaultDirectory => "Assets/Art/Svg/";
    }
}
