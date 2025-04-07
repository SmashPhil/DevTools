using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DevTools.UnitTesting;

public static class Expect
{
  public static event Action<string, bool> OnResult;

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
    if (!UnitTestManager.RunningUnitTests)
    {
      Trace.Fail(
        "Using Expect outside of test watcher. Use Trace or Assert instead, Expect is exclusively " +
        "for unit testing.");
      return;
    }
    OnResult?.Invoke(label, result);
  }

  internal readonly struct TestWatcher : IDisposable
  {
    private readonly List<(string, bool)> outputList;

    public TestWatcher(List<(string, bool)> outputList)
    {
      this.outputList = outputList;
      OnResult += OutputSignalToList;
    }

    void IDisposable.Dispose()
    {
      OnResult -= OutputSignalToList;
    }

    private void OutputSignalToList(string label, bool result)
    {
      outputList.Add((label, result));
    }
  }
}