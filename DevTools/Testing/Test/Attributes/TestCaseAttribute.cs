using System;
using JetBrains.Annotations;

namespace DevTools.Testing;

/// <summary>
/// Marks a method as a test case with inline arguments.
/// </summary>
/// <param name="args">Arguments supplied to the test method.</param>
[PublicAPI]
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class TestCaseAttribute(params object[] args) : TestAttribute
{
  /// <summary>
  /// Arguments supplied to the test method.
  /// </summary>
  internal object[] Arguments { get; } = args;

  /// <summary>
  /// Expected test result.
  /// </summary>
  public object ExpectedResult { get; set; }
}
