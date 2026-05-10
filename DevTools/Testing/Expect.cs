using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine.Assertions.Comparers;
using Verse;

namespace DevTools.Testing;

[PublicAPI]
public static class Expect
{
  public static void That(Func<bool> validator, string message = null)
  {
    Signal(validator(), nameof(That), message);
  }

  public static void IsTrue(bool condition, string message = null)
  {
    Signal(condition, nameof(IsTrue), message, MessageBuilder.BooleanFailureMessage(true));
  }

  public static void IsFalse(bool condition, string message = null)
  {
    Signal(!condition, nameof(IsFalse), message, MessageBuilder.BooleanFailureMessage(false));
  }

  public static void IsEmpty<T>(IEnumerable<T> collection, string message = null)
  {
    Signal(!collection.Any(), nameof(IsEmpty), message);
  }

  public static void IsNotEmpty<T>(IEnumerable<T> collection, string message = null)
  {
    Signal(collection.Any(), nameof(IsNotEmpty), message);
  }

  public static void All<T>(IEnumerable<T> items, Func<T, bool> validator, string message = null)
  {
    Signal(items.All(validator), nameof(All), message);
  }

  public static void Any<T>(IEnumerable<T> items, Func<T, bool> validator, string message = null)
  {
    Signal(items.Any(validator), nameof(Any), message);
  }

  public static void None<T>(IEnumerable<T> items, Func<T, bool> validator, string message = null)
  {
    Signal(!items.Any(validator), nameof(None), message);
  }

  public static void AreApproximatelyEqual(float expected, float actual, string message = null)
  {
    Signal(FloatComparer.s_ComparerWithDefaultTolerance.Equals(expected, actual),
      nameof(AreApproximatelyEqual), message,
      MessageBuilder.GetEqualityMessage(expected, actual, expectEqual: true));
  }

  public static void AreNotApproximatelyEqual(float expected, float actual, string message = null)
  {
    Signal(!FloatComparer.s_ComparerWithDefaultTolerance.Equals(expected, actual),
      nameof(AreNotApproximatelyEqual), message,
      MessageBuilder.GetEqualityMessage(expected, actual, expectEqual: false));
  }

  public static void ReferencesAreEqual<T>(T expected, T actual, string message = null) where T : class
  {
    Signal(ReferenceEquals(expected, actual), nameof(ReferencesAreEqual), message,
      MessageBuilder.GetEqualityMessage(expected, actual, expectEqual: true));
  }

  public static void ReferencesAreNotEqual<T>(T expected, T actual, string message = null) where T : class
  {
    Signal(!ReferenceEquals(expected, actual), nameof(ReferencesAreNotEqual), message,
      MessageBuilder.GetEqualityMessage(expected, actual, expectEqual: false));
  }

  public static void AreEqual<T>(T expected, T actual, string message = null)
  {
    Signal(EqualityComparer<T>.Default.Equals(expected, actual), nameof(AreEqual), message,
      MessageBuilder.GetEqualityMessage(expected, actual, expectEqual: true));
  }

  public static void AreNotEqual<T>(T expected, T actual, string message = null)
  {
    Signal(!EqualityComparer<T>.Default.Equals(expected, actual), nameof(AreNotEqual), message,
      MessageBuilder.GetEqualityMessage(expected, actual, expectEqual: false));
  }

  public static void GreaterThan<T>(T value, T operand, string message = null) where T : IComparable
  {
    Signal(value.CompareTo(operand) > 0, nameof(GreaterThan), message,
      MessageBuilder.GetComparisonMessage(value, operand, MessageBuilder.NumericalComparison.GreaterThan));
  }

  public static void GreaterThanOrEqualTo<T>(T value, T operand, string message = null)
    where T : IComparable
  {
    Signal(value.CompareTo(operand) > 0 || EqualityComparer<T>.Default.Equals(value, operand),
      nameof(GreaterThanOrEqualTo), message,
      MessageBuilder.GetComparisonMessage(value, operand, MessageBuilder.NumericalComparison.GreaterThanOrEqual));
  }

  public static void LessThan<T>(T value, T operand, string message = null) where T : IComparable
  {
    Signal(value.CompareTo(operand) < 0, nameof(LessThan), message,
      MessageBuilder.GetComparisonMessage(value, operand, MessageBuilder.NumericalComparison.LessThan));
  }

  public static void LessThanOrEqualTo<T>(T value, T operand, string message = null) where T : IComparable
  {
    Signal(value.CompareTo(operand) < 0 || EqualityComparer<T>.Default.Equals(value, operand),
      nameof(LessThanOrEqualTo), message,
      MessageBuilder.GetComparisonMessage(value, operand, MessageBuilder.NumericalComparison.LessThanOrEqual));
  }

  public static void IsNull<T>(T obj, string message = null) where T : class
  {
    Signal(obj == null, nameof(IsNull), message, MessageBuilder.NullFailureMessage(obj, expectNull: true));
  }

  public static void IsNotNull<T>(T obj, string message = null) where T : class
  {
    Signal(obj != null, nameof(IsNotNull), message, MessageBuilder.NullFailureMessage(obj, expectNull: false));
  }

  [DebuggerHidden]
  public static T Throws<T>(Action action, string message = null) where T : Exception
  {
    T exception = null;
    try
    {
      action();
    }
    catch (T ex)
    {
      exception = ex;
    }
    Signal(exception != null, nameof(Throws), message);
    return exception;
  }

  private static void Signal(bool result, string context, string label,
    string failureMessage = null)
  {
    SendSignal(result ? Status.Passed : Status.Failed, context, label, failureMessage,
      skipFrames: 3);
  }

  internal static void SendSignal(Status status, string context, string message,
    string failureMessage = null, int skipFrames = 1)
  {
    if (!TestRunner.Active)
    {
      Log.Error(
        "Using Expect outside of test watcher. Use Assert instead, Expect is exclusively for unit testing.");
      return;
    }

    ITestGroup current = Test.Current;
    current.Status = status;
    if (status is Status.Failed or Status.Canceled or Status.Skipped)
    {
      current.TestContext ??= context;
      current.FailLabel ??= message;
      current.FailMessage ??= failureMessage;
      StackTrace stackTrace = new(skipFrames, true);
      if (status is Status.Failed && Debugger.IsAttached && TestFixtureManager.BreakOnTestFailure)
      {
        Debugger.Break();
      }
      current.StackTrace ??= stackTrace;
    }
  }
}