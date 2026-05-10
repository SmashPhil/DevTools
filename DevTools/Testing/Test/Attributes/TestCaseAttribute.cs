using System;
using JetBrains.Annotations;

namespace DevTools.Testing;

[PublicAPI]
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class TestCaseAttribute(params object[] args) : TestAttribute
{
  public object[] Arguments { get; } = args;

  public object ExpectedResult { get; set; }
}