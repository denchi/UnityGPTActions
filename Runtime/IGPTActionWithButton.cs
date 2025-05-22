public interface IGPTActionWithButton : IGPTAction
{
    string ButtonTitle { get; }
    void OnClick();
}