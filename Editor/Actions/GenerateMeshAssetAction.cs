namespace GPTUnity.Actions
{
    [GPTAction(@"Creates a mesh asset from Wavefront OBJ content. Use this when you already have explicit OBJ data to import into the Unity project.", Name = "create_mesh_asset_from_obj")]
    public class GenerateMeshAssetAction : CreateFileActionBase
    {
        [GPTParameter("Wavefront OBJ file contents to save as the mesh source asset.", true, Name = "obj_content")]
        public string ObjContent { get; set; }

        [GPTParameter("Optional human-readable note describing the mesh. This is metadata only and does not generate geometry.", Name = "mesh_description")]
        public string MeshDescription { get; set; }

        public override string Content => ObjContent;
        protected override string DefaultFileExtension => ".obj";
        protected override string DefaultDirectory => "Assets/Models/";
    }
}
