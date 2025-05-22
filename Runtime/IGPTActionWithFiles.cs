public interface IGPTActionWithFiles : IGPTAction
{
    string FileName { get; }
    string FileExtension { get; }
    string PathToDirectory { get; }
    void CreateFile(string overridePath = null);
}