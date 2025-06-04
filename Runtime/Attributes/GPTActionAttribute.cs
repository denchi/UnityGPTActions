using System;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class GPTActionAttribute : Attribute
{
    public string Description { get; set; }
    public ActionRunMode Mode { get; set; }
    
    public GPTActionAttribute() { }

    public GPTActionAttribute(string description)
    {
        Description = description;
    }
    
    public GPTActionAttribute(string description, ActionRunMode mode)
    {
        Description = description;
        Mode = mode;
    }
}

[Flags]
public enum ActionRunMode
{
    None = 0,
    Editor = 1 << 0,
    Player = 1 << 1,
    Both = Editor | Player
}