using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DevTools.UnitTesting;

public static class Expect
{
  public static event Action<string, Status> OnResult;

  public static void IsTrue(string label, bool condition)
  {
    Signal(label, condition);
  }

  public static void IsFalse(string label, bool condition)
  {
    Signal(label, !condition);
  }

  public static void IsNull<T>(string label, T obj) where T : class
  {
    Signal(label, obj == null);
  }

  public static void IsNotNull<T>(string label, T obj) where T : class
  {
    Signal(label, obj != null);
  }

  private static void Signal(string label, bool result)
  {
    Signal(label, result ? Status.Passed : Status.Failed);
  }

  internal static void Signal(string label, Status status)
  {
    if (!UnitTestManager.RunningUnitTests)
    {
      Trace.Fail(
        "Using Expect outside of test watcher. Use Trace or Assert instead, Expect is exclusively " +
        "for unit testing.");
      return;
    }
    OnResult?.Invoke(label, status);
  }

  internal readonly struct TestWatcher : IDisposable
  {
    private readonly List<(string, Status)> outputList;

    public TestWatcher(List<(string, Status)> outputList)
    {
      this.outputList = outputList;
      OnResult += OutputSignalToList;
    }

    void IDisposable.Dispose()
    {
      OnResult -= OutputSignalToList;
    }

    private void OutputSignalToList(string label, Status status)
    {
      outputList.Add((label, status));
    }
  }
}