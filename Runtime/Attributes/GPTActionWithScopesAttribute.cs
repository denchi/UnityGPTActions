using System;

[AttributeUsage(AttributeTargets.Class)]
public sealed class GPTActionWithScopesAttribute : Attribute
{
    public string[] Scopes { get; set; }
    
    public GPTActionWithScopesAttribute() { }

    public GPTActionWithScopesAttribute(params string[] scopes)
    {
        Scopes = scopes;
    }
}