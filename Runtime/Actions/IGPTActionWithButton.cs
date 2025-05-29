namespace GPTUnity.Actions.Interfaces
{
    public interface IGPTActionWithButton
    {
        string ButtonTitle { get; }
        void OnClick();
    }
}