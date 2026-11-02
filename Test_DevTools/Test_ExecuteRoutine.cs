using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DevTools.Testing;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Assertions;

namespace DevTools.Tests;

/// <summary>
/// Steps <see cref="TestFunction.ExecuteRoutine(object, Action{Exception})"/> by hand and records
/// exceptions instead of failing the running test, so an exception that escapes the routine fails
/// these tests rather than stopping the test runner.
/// </summary>
[TestFixture(TestType.MainMenu)]
internal class Test_ExecuteRoutine
{
  private static readonly List<string> events = [];

  [SetUp]
  private void ClearEvents()
  {
    events.Clear();
  }

  [Test]
  private void ExceptionInYieldedCoroutine()
  {
    Exception ex = RunExpectingException(nameof(ThrowsOneLevelDown));
    Assert.AreEqual("one level", ex.Message);
  }

  [Test]
  private void ExceptionTwoLevelsDown()
  {
    Exception ex = RunExpectingException(nameof(ThrowsTwoLevelsDown));
    Assert.AreEqual("two levels", ex.Message);
  }

  [Test]
  private void ExceptionInYieldInstruction()
  {
    Exception ex = RunExpectingException(nameof(YieldsThrowingInstruction));
    Assert.AreEqual("instruction", ex.Message);
  }

  [Test]
  private void ExceptionBeforeFirstYield()
  {
    Exception ex = RunExpectingException(nameof(ThrowsWhenInvoked));
    Assert.AreEqual("invoked", ex.GetBaseException().Message);
  }

  [Test]
  private void ExceptionDisposesSuspendedRoutines()
  {
    RunExpectingException(nameof(ThrowsInsideFinallyBlocks));
    AssertEvents(["inner finally", "outer finally"]);
  }

  [Test]
  private void NestedCoroutineFinishesAndOuterContinues()
  {
    IEnumerator routine = Create(nameof(NestedThenContinues), RecordUnexpected);

    Assert.IsTrue(routine.MoveNext());
    Assert.IsNull(routine.Current);
    AssertEvents(["outer start", "inner start"]);

    // The parent resumes in the same step as the nested routine ends, up to its own next yield.
    Assert.IsTrue(routine.MoveNext());
    Assert.IsNull(routine.Current);
    AssertEvents(["outer start", "inner start", "inner end", "outer resumed"]);

    Assert.IsFalse(routine.MoveNext());
    AssertEvents(["outer start", "inner start", "inner end", "outer resumed", "outer end"]);
  }

  [Test]
  private void YieldInstructionWaitsOneStepAtATime()
  {
    IEnumerator routine = Create(nameof(YieldsCountedInstruction), RecordUnexpected);

    for (int i = 0; i < CountedInstruction.Steps; i++)
    {
      Assert.IsTrue(routine.MoveNext());
      Assert.IsNull(routine.Current);
    }
    Assert.IsFalse(routine.MoveNext());
    AssertEvents(["after instruction"]);
  }

  [Test]
  private void SchedulerYieldsPassThrough()
  {
    IEnumerator routine = Create(nameof(YieldsSchedulerInstruction), RecordUnexpected);

    Assert.IsTrue(routine.MoveNext());
    Assert.IsTrue(ReferenceEquals(schedulerInstruction, routine.Current));
    Assert.IsFalse(routine.MoveNext());
  }

  [Test]
  private void NullRoutineFinishes()
  {
    Assert.IsFalse(Create(nameof(ReturnsNull), RecordUnexpected).MoveNext());
  }

  /// <summary>
  /// Runs under the real scheduler, where a routine that lets an exception escape stops the runner
  /// and never reaches the assertion.
  /// </summary>
  [Test]
  private IEnumerator ContinuesUnderSchedulerAfterException()
  {
    Exception caught = null;
    yield return Create(nameof(ThrowsOneLevelDown), ex => caught = ex);
    Assert.IsNotNull(caught);
    Assert.AreEqual("one level", caught.Message);
  }

  private static Exception RunExpectingException(string method)
  {
    List<Exception> caught = [];
    IEnumerator routine = Create(method, caught.Add);
    while (routine.MoveNext())
    {
      Assert.IsFalse(routine.Current is IEnumerator, "Nested routine was handed to the scheduler.");
    }
    Assert.AreEqual(1, caught.Count);
    return caught[0];
  }

  private static IEnumerator Create(string method, Action<Exception> onException)
  {
    MethodInfo methodInfo = AccessTools.Method(typeof(Test_ExecuteRoutine), method);
    return new TestFunction(fixture: null, methodInfo, Testing.MethodType.Test).ExecuteRoutine(instance: null, onException);
  }

  private static void RecordUnexpected(Exception ex)
  {
    Assert.IsNull(ex, $"Unexpected exception: {ex}");
  }

  private static void AssertEvents(string[] expected)
  {
    Assert.AreEqual(string.Join(", ", expected), string.Join(", ", events));
  }

  private static readonly WaitForEndOfFrame schedulerInstruction = new();

  private static IEnumerator ThrowsOneLevelDown()
  {
    yield return Throw("one level");
  }

  private static IEnumerator ThrowsTwoLevelsDown()
  {
    yield return ThrowsNested();

    static IEnumerator ThrowsNested()
    {
      yield return Throw("two levels");
    }
  }

  private static IEnumerator Throw(string message)
  {
    yield return null;
    throw new InvalidOperationException(message);
  }

  private static IEnumerator YieldsThrowingInstruction()
  {
    yield return new ThrowingInstruction();
  }

  private static IEnumerator ThrowsWhenInvoked()
  {
    throw new InvalidOperationException("invoked");
  }

  private static IEnumerator ThrowsInsideFinallyBlocks()
  {
    try
    {
      yield return ThrowsInFinally();
    }
    finally
    {
      events.Add("outer finally");
    }

    static IEnumerator ThrowsInFinally()
    {
      try
      {
        yield return null;
        throw new InvalidOperationException("finally");
      }
      finally
      {
        events.Add("inner finally");
      }
    }
  }

  private static IEnumerator NestedThenContinues()
  {
    events.Add("outer start");
    yield return Nested();
    events.Add("outer resumed");
    yield return null;
    events.Add("outer end");

    static IEnumerator Nested()
    {
      events.Add("inner start");
      yield return null;
      events.Add("inner end");
    }
  }

  private static IEnumerator YieldsCountedInstruction()
  {
    yield return new CountedInstruction();
    events.Add("after instruction");
  }

  private static IEnumerator YieldsSchedulerInstruction()
  {
    yield return schedulerInstruction;
  }

  private static IEnumerator ReturnsNull()
  {
    return null;
  }

  private sealed class ThrowingInstruction : CustomYieldInstruction
  {
    public override bool keepWaiting => throw new InvalidOperationException("instruction");
  }

  private sealed class CountedInstruction : CustomYieldInstruction
  {
    public const int Steps = 3;

    private int remaining = Steps;

    public override bool keepWaiting => remaining-- > 0;
  }
}
