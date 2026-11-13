using System.Linq;
using DevTools.Testing;
using UnityEngine;
using UnityEngine.Assertions;

namespace DevTools.Tests;

[TestFixture(TestType.MainMenu)]
[TestDescription("Allows a log only when its stack trace holds the given frame too, so one mod's known warning does not hide the same warning from another.")]
internal class Test_LogsAllowedFrom
{
  private const string Frame = @"OtherMod\.PawnTable\.ExposeData";
  private const string Warning = "Could not resolve reference to Thing with loadID Thing_Human123";
  private const string Error = "Exception getting object with thing id 123 of type Verse.Pawn";

  private const string ThirdFrame = @"ThirdMod\.Diet\.ExposeData";
  private const string ThirdWarning = "Could not resolve reference to Pawn with loadID Thing_Human456";

  private const string OtherModStack =
    "    at Verse.Log.Warning(String text) [0x0]\n" +
    "    at Verse.Scribe_References.Look[Pawn](Pawn& refee, String label, Boolean saveDestroyedThings) [0x5C]\n" +
    "    at OtherMod.PawnTable.ExposeData() [0x1A]\n" +
    "    at Verse.CrossRefHandler.ResolveAllCrossReferences() [0x88]\n";

  private const string ModUnderTestStack =
    "    at Verse.Log.Warning(String text) [0x0]\n" +
    "    at Verse.Scribe_References.Look[Pawn](Pawn& refee, String label, Boolean saveDestroyedThings) [0x5C]\n" +
    "    at ModUnderTest.CompOwner.PostExposeData() [0x10]\n" +
    "    at Verse.CrossRefHandler.ResolveAllCrossReferences() [0x88]\n";

  private const string ThirdModStack =
    "    at Verse.Log.Warning(String text) [0x0]\n" +
    "    at Verse.Scribe_References.Look[Pawn](Pawn& refee, String label, Boolean saveDestroyedThings) [0x5C]\n" +
    "    at ThirdMod.Diet.ExposeData() [0x22]\n" +
    "    at Verse.CrossRefHandler.ResolveAllCrossReferences() [0x88]\n";

  [Test, WarningsAllowedFrom(Frame, Warning), ErrorsAllowedFrom(Frame, Error)]
  private void AllowedWithFrame()
  {
    ITestCase scope = Test.Current.TestCase;
    LogWatcher.TestLogEntry(new TestConfig(), LogType.Warning, new LogWatcher.LogEntry(Warning, OtherModStack), scope);
    LogWatcher.TestLogEntry(new TestConfig(), LogType.Error, new LogWatcher.LogEntry(Error, OtherModStack), scope);
  }

  [Test, WarningsAllowedFrom(Frame, Warning), ErrorsAllowedFrom(Frame, Error)]
  private void NotAllowedWithoutFrame()
  {
    ITestCase[] scopes = [Test.Current.TestCase];
    Assert.IsFalse(LogWatcher.AllowedByScope(scopes, LogType.Warning, Warning, ModUnderTestStack));
    Assert.IsFalse(LogWatcher.AllowedByScope(scopes, LogType.Error, Error, ModUnderTestStack));
    Assert.IsFalse(LogWatcher.AllowedByScope(scopes, LogType.Warning, Warning, string.Empty));
    Assert.IsFalse(LogWatcher.AllowedByScope(scopes, LogType.Warning, "Unrelated warning", OtherModStack));
    Assert.IsFalse(LogWatcher.AllowedByScope(scopes, LogType.Error, Warning, OtherModStack));
  }

  [Test, WarningsAllowedFrom("", Warning), ErrorsAllowedFrom(".*", Error)]
  private void MatchAllStackPatternAllowsNothing()
  {
    ITestCase[] scopes = [Test.Current.TestCase];
    Assert.IsFalse(LogWatcher.AllowedByScope(scopes, LogType.Warning, Warning, OtherModStack));
    Assert.IsFalse(LogWatcher.AllowedByScope(scopes, LogType.Error, Error, OtherModStack));
  }

  [Test, WarningsAllowed(Warning), ErrorsAllowed(Error)]
  private void MessageOnlyIgnoresStack()
  {
    ITestCase[] scopes = [Test.Current.TestCase];
    Assert.IsTrue(LogWatcher.AllowedByScope(scopes, LogType.Warning, Warning, OtherModStack));
    Assert.IsTrue(LogWatcher.AllowedByScope(scopes, LogType.Warning, Warning, ModUnderTestStack));
    Assert.IsTrue(LogWatcher.AllowedByScope(scopes, LogType.Error, Error, string.Empty));
  }

  [Test, WarningsAllowedFrom(Frame, Warning), WarningsAllowedFrom(ThirdFrame, ThirdWarning)]
  private void SeveralSourcesKeepTheirOwnPatterns()
  {
    ITestCase[] scopes = [Test.Current.TestCase];
    Assert.IsTrue(LogWatcher.AllowedByScope(scopes, LogType.Warning, Warning, OtherModStack));
    Assert.IsTrue(LogWatcher.AllowedByScope(scopes, LogType.Warning, ThirdWarning, ThirdModStack));
    Assert.IsFalse(LogWatcher.AllowedByScope(scopes, LogType.Warning, Warning, ThirdModStack));
    Assert.IsFalse(LogWatcher.AllowedByScope(scopes, LogType.Warning, ThirdWarning, OtherModStack));
  }

  [Test]
  private void RepeatedAllowancesAddUpThroughInheritance()
  {
    MetaDataContainer metaData = new();
    metaData.Load(typeof(DerivedScope));
    Assert.AreEqual(2, metaData.GetAll<StackAllowance>(MetaDataName.WarningsAllowedFrom).Count());
    Assert.AreEqual(2, metaData.GetAll<StackAllowance>(MetaDataName.ErrorsAllowedFrom).Count());
  }

  [WarningsAllowedFrom(Frame, Warning), ErrorsAllowedFrom(Frame, Error)]
  private class BaseScope;

  [WarningsAllowedFrom(ThirdFrame, ThirdWarning), ErrorsAllowedFrom(ThirdFrame, Error)]
  private class DerivedScope : BaseScope;
}
