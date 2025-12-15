using System;
using JetBrains.Annotations;

namespace DevTools.Testing;

[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method)]
public class TestAttribute : Attribute
{
  public TestAttribute()
  {
  }

  public TestAttribute(string name)
  {
    Name = name;
  }

  public string Name { get; }
}