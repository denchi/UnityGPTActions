using System;

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class GPTParameterAttribute : Attribute
{
    public string Description { get; }
    public bool Required { get; }
    //public string SerializeFunction { get; }

    public GPTParameterAttribute(string description /*, string actionName = null*/)
    {
        Description = description;
        //SerializeFunction = actionName;
    }
    
    public GPTParameterAttribute(string description, bool required /*, string actionName = null*/)
    {
        Description = description;
        Required = required;
        //SerializeFunction = actionName;
    }
}