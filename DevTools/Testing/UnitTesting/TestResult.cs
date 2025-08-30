using System.Diagnostics;

namespace DevTools.UnitTesting;

#nullable enable

internal readonly struct TestResult
{
  public readonly Status status;
  public readonly string label;
  public readonly string message;
  public readonly StackFrame? stackFrame;

  public TestResult(Status status, string label, string message, StackFrame? stackFrame)
  {
    this.status = status;
    this.label = label;
    this.message = message;
    this.stackFrame = stackFrame;
  }
}