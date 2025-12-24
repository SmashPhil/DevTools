using System;
using JetBrains.Annotations;

namespace DevTools.Testing;

[PublicAPI]
[AttributeUsage(AttributeTargets.Class)]
public class TestClassAttribute : Attribute
{
  public TestClassAttribute(TestType type)
  {
    Type = type;
  }

  public TestType Type { get; }
}
