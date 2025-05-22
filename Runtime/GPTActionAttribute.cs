using System;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class GPTActionAttribute : Attribute
{
    public string Description { get; set; }
    public GPTActionAttribute() { }

    public GPTActionAttribute(string description)
    {
        Description = description;
    }
}