namespace GPTUnity.Actions.Interfaces
{
    public interface IGPTActionThatRequiresPermission
    {
        // What should the user be asked? in the editor gui? Example Create File? 
        string Question { get; }
    }
}