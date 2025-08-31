using System;

namespace DevTools.Testing;

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