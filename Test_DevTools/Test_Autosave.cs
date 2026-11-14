using System;
using System.Collections;
using System.Diagnostics;
using DevTools.Testing;
using HarmonyLib;
using RimWorld;
using UnityEngine.Assertions;
using Verse;

namespace DevTools.Tests;

[TestFixture(TestType.Playing)]
[TestDescription("Holds the game's autosave during tests, unless a test or fixture allows it.")]
internal class Test_Autosave
{
  private const int TimeoutSeconds = 30;

  private static readonly AccessTools.FieldRef<Autosaver, int> TicksSinceSave =
    AccessTools.FieldRefAccess<Autosaver, int>("ticksSinceSave");

  private static readonly Func<Autosaver, int> IntervalTicks = AccessTools.MethodDelegate<Func<Autosaver, int>>(
    AccessTools.PropertyGetter(typeof(Autosaver), "AutosaveIntervalTicks"));

  internal static int Autosaves { get; private set; }

  /// <summary>
  /// Runs one tick from one tick short of the autosave interval and counts the autosaves the game
  /// starts, skipping the save itself so that no file is written.
  /// </summary>
  internal static IEnumerator TickToTheInterval()
  {
    Autosaves = 0;
    Autosaver autosaver = Find.Autosaver;
    int ticksSinceSave = TicksSinceSave(autosaver);
    using ScopedMethodHook hook = new(AccessTools.Method(typeof(Autosaver), nameof(Autosaver.DoAutosave)),
      prefix: new HarmonyMethod(typeof(Test_Autosave), nameof(CountAutosave)));
    try
    {
      TicksSinceSave(autosaver) = IntervalTicks(autosaver) - 1;
      Find.TickManager.DoSingleTick();
      Stopwatch waiting = Stopwatch.StartNew();
      while (LongEventHandler.AnyEventNowOrWaiting)
      {
        Assert.IsTrue(waiting.Elapsed.TotalSeconds < TimeoutSeconds, "Timed out waiting for the autosave.");
        yield return null;
      }
    }
    finally
    {
      TicksSinceSave(autosaver) = ticksSinceSave;
    }
  }

  private static bool CountAutosave()
  {
    Autosaves++;
    return false;
  }

  [Test]
  private IEnumerator HeldByDefault()
  {
    yield return TickToTheInterval();
    Assert.AreEqual(0, Autosaves);
  }

  [Test, AutosaveAllowed]
  private IEnumerator AllowedByTheTest()
  {
    yield return TickToTheInterval();
    Assert.AreEqual(1, Autosaves);
  }

  [Test, ExecutionPriority(-1)]
  private IEnumerator HeldAgainAfterATestThatAllowedIt()
  {
    yield return TickToTheInterval();
    Assert.AreEqual(0, Autosaves);
  }
}

[TestFixture(TestType.Playing), AutosaveAllowed]
[TestDescription("Lets the game autosave during every test of a fixture that allows it.")]
internal class Test_AutosaveAllowedFixture
{
  [Test]
  private IEnumerator AllowedByTheFixture()
  {
    yield return Test_Autosave.TickToTheInterval();
    Assert.AreEqual(1, Test_Autosave.Autosaves);
  }
}
