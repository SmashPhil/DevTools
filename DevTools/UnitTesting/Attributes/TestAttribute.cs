using System;

namespace DevTools.UnitTesting;

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