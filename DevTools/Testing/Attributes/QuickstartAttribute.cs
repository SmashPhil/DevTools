using System;

namespace DevTools.Testing;

[AttributeUsage(AttributeTargets.Class)]
public class QuickstartAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}