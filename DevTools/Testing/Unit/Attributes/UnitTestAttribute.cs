using System;

namespace DevTools.Testing;

[AttributeUsage(AttributeTargets.Class)]
public class UnitTestAttribute : Attribute
{
  public UnitTestAttribute(TestType type)
  {
    Type = type;
  }

  public TestType Type { get; }
}