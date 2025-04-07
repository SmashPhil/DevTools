using System;

namespace DevTools.UnitTesting;

[AttributeUsage(AttributeTargets.Class)]
public class UnitTestAttribute : Attribute
{
  public UnitTestAttribute(TestType type)
  {
    Type = type;
  }

  public TestType Type { get; }

  public string Category { get; set; }

  public bool RunAsync { get; set; } = true;
}