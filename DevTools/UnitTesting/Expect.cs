using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using Verse;

namespace DevTools.UnitTesting;

public static class Expect
{
  public static void That(Func<bool> validator, string message = null)
  {
    Signal(validator(), "Expect.That", message);
  }

  public static void IsTrue(bool condition, string message = null)
  {
    Signal(condition, "Expect.IsTrue", message);
  }

  public static void IsFalse(bool condition, string message = null)
  {
    Signal(!condition, "Expect.IsFalse", message);
  }

  public static void IsEmpty<T>(IEnumerable<T> collection, string message = null)
  {
    Signal(!collection.Any(), "Expect.IsEmpty", message);
  }

  public static void IsNotEmpty<T>(IEnumerable<T> collection, string message = null)
  {
    Signal(collection.Any(), "Expect.IsNotEmpty", message);
  }

  public static void All<T>(IEnumerable<T> items, Func<T, bool> validator, string message = null)
  {
    Signal(items.All(validator), "Expect.All", message);
  }

  public static void Any<T>(IEnumerable<T> items, Func<T, bool> validator, string message = null)
  {
    Signal(items.Any(validator), "Expect.Any", message);
  }

  public static void None<T>(IEnumerable<T> items, Func<T, bool> validator, string message = null)
  {
    Signal(!items.Any(validator), "Expect.None", message);
  }

  public static void ApproximatelyEqual(float lhs, float rhs, string message = null)
  {
    Signal(Mathf.Approximately(lhs, rhs), "Expect.ApproximatelyEqual", message);
  }

  public static void ApproximatelyNotEqual(float lhs, float rhs, string message = null)
  {
    Signal(!Mathf.Approximately(lhs, rhs), "Expect.ApproximatelyNotEqual", message);
  }

  public static void ReferencesAreEqual<T>(T lhs, T rhs, string message = null) where T : class
  {
    Signal(ReferenceEquals(lhs, rhs), "Expect.ReferencesAreEqual", message);
  }

  public static void ReferencesAreNotEqual<T>(T lhs, T rhs, string message = null) where T : class
  {
    Signal(!ReferenceEquals(lhs, rhs), "Expect.ReferencesAreNotEqual", message);
  }

  public static void AreEqual<T>(T lhs, T rhs, string message = null)
  {
    Signal(EqualityComparer<T>.Default.Equals(lhs, rhs), "Expect.AreEqual", message);
  }

  public static void AreNotEqual<T>(T lhs, T rhs, string message = null)
  {
    Signal(!EqualityComparer<T>.Default.Equals(lhs, rhs), "Expect.AreNotEqual", message);
  }

  public static void GreaterThan<T>(T lhs, T rhs, string message = null) where T : IComparable
  {
    Signal(lhs.CompareTo(rhs) > 0, "Expect.GreaterThan", message);
  }

  public static void GreaterThanOrEqualTo<T>(T lhs, T rhs, string message = null)
    where T : IComparable
  {
    Signal(lhs.CompareTo(rhs) > 0 || EqualityComparer<T>.Default.Equals(lhs, rhs),
      "Expect.GreaterThanOrEqualTo",
      message);
  }

  public static void LessThan<T>(T lhs, T rhs, string message = null) where T : IComparable
  {
    Signal(lhs.CompareTo(rhs) < 0, "Expect.LessThan", message);
  }

  public static void LessThanOrEqualTo<T>(T lhs, T rhs, string message = null) where T : IComparable
  {
    Signal(lhs.CompareTo(rhs) < 0 || EqualityComparer<T>.Default.Equals(lhs, rhs),
      "Expect.LessThanOrEqualTo",
      message);
  }

  public static void IsNull<T>(T obj, string message = null) where T : class
  {
    Signal(obj == null, "Expect.IsNull", message);
  }

  public static void IsNotNull<T>(T obj, string message = null) where T : class
  {
    Signal(obj != null, "Expect.IsNotNull", message);
  }

  [DebuggerHidden]
  public static void Throws<T>(Action action, string message = null) where T : Exception
  {
    bool threw = false;
    try
    {
      action();
    }
    catch (T)
    {
      threw = true;
    }
    Signal(threw, "Expect.Throws", message);
  }

  private static void Signal(bool result, string label, string message)
  {
    SendSignal(result ? Status.Passed : Status.Failed, label, message, skipFrames: 3);
  }

  internal static void SendSignal(Status status, string label, string message, int skipFrames = 1)
  {
    if (!UnitTestManager.RunningUnitTests)
    {
      Log.Error(
        "Using Expect outside of test watcher. Use Assert instead, Expect is exclusively for unit testing.");
      return;
    }
    StackFrame stackFrame = null;
    if (status is Status.Skipped or Status.Canceled or Status.Failed)
    {
      stackFrame = new StackTrace(skipFrames, true).GetFrame(0);
      if (status is Status.Failed && Debugger.IsAttached && UnitTestManager.breakOnTestFailure)
        Debugger.Break();
    }
    Test.Log(StatusMessage(status, label, message));
    Test.CurrentGroup.Results.Add(new TestResult(status, label, message, stackFrame));
  }

  private static string StatusMessage(Status status, string label, string message)
  {
    return status switch
    {
      Status.Failed   => $"[Failed]    {label}   {message}",
      Status.Canceled => $"[Canceled]  {label}   {message}",
      Status.Skipped  => $"[Skipped]   {label}   {message}",
      Status.Passed   => $"[Passed]    {label}   {message}",
      Status.Pending  => $"[Pending]   {label}   {message}",
      Status.NotRun   => $"[NotRun]    {label}   {message}",
      _               => throw new NotImplementedException(nameof(Status)),
    };
  }
}