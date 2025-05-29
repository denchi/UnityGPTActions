using System;

[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
public sealed class GPTRequiresPackageAttribute : Attribute
{
    public string PackageName { get; set; }
    
    public GPTRequiresPackageAttribute(string packageName)
    {
        PackageName = packageName;
    }
}