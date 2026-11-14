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
  internal static readonly AccessTools.FieldRef<Autosaver, int> TicksSinceSave =
    AccessTools.FieldRefAccess<Autosaver, int>("ticksSinceSave");

  /// <summary>
  /// Runs one game tick from a counter of zero and returns the counter after it: 0 when the
  /// autosave tick was skipped, 1 when it ran.
  /// </summary>
  internal static int CounterAfterOneTick()
  {
    TicksSinceSave(Find.Autosaver) = 0;
    Find.TickManager.DoSingleTick();
    return TicksSinceSave(Find.Autosaver);
  }

  [Test]
  private void HeldByDefault()
  {
    Assert.IsTrue(TestRunner.AutosaveHeld);
    Assert.AreEqual(0, CounterAfterOneTick());
  }

  [Test, AutosaveAllowed]
  private void AllowedByTheTest()
  {
    Assert.IsFalse(TestRunner.AutosaveHeld);
    Assert.AreEqual(1, CounterAfterOneTick());
  }

  [Test]
  private void HeldAgainAfterATestThatAllowedIt()
  {
    Assert.IsTrue(TestRunner.AutosaveHeld);
    Assert.AreEqual(0, CounterAfterOneTick());
  }
}

[TestFixture(TestType.Playing), AutosaveAllowed]
[TestDescription("Lets the game autosave during every test of a fixture that allows it.")]
internal class Test_AutosaveAllowedFixture
{
  [Test]
  private void AllowedByTheFixture()
  {
    Assert.IsFalse(TestRunner.AutosaveHeld);
    Assert.AreEqual(1, Test_Autosave.CounterAfterOneTick());
  }
}
