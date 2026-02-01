using System;
using JetBrains.Annotations;

namespace DevTools.Testing;

[PublicAPI, MeansImplicitUse]
[AttributeUsage(AttributeTargets.Class)]
public class UnitTestAttribute : TestClassAttribute
{
  public UnitTestAttribute(TestType type) : base(type)
  {
  }
}