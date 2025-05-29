namespace GPTUnity.Actions.Interfaces
{
    public interface IGPTActionWithFiles
    {
        string FileName { get; }
        string FileExtension { get; }
        string PathToDirectory { get; }
        void CreateFile(string overridePath = null);
    }
}