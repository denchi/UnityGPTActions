public interface IGPTActionWithFiles : IGPTAction
{
    string FileName { get; }
    string FileExtension { get; }
    string PathToDirectory { get; }
    string Content { get; }
    void CreateFile(string overridePath = null);
}