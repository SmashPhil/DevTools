using System;
using System.Collections;
using System.Threading;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;

namespace DevTools.UnitTesting;

[PublicAPI]
public static class Test
{
  // There should always be 1 group if testing is in progress, an empty one will be used
  // as the root test group of the test method.
  internal static ContextGroup CurrentGroup { get; private set; }

  public static void BeginGroup(string name)
  {
    bool invalidName = name.NullOrEmpty();
    // If CurrentGroup is null, we're opening a group for the root test
    if (invalidName && CurrentGroup != null)
    {
      // Must send to player.log before throwing since test exceptions are caught and logged to
      // the test log, and NOT the player.log, this is primarily for visibility and clear separation
      // from game logs, but this is a user-error that should be visible in the player log.
      Log.Error("Attempting to open empty Test.Group, this is not allowed.");
      throw new ArgumentException("Empty group name");
    }
    if (!invalidName)
      DevLog.WriteVerbose($"-- Begin Group ({name})");
    ContextGroup group = new(name, CurrentGroup);
    CurrentGroup?.Groups.Add(group);
    CurrentGroup = group;
    CurrentGroup.Open();
  }

  public static void EndGroup(string name)
  {
    bool invalidName = name.NullOrEmpty();
    Assert.IsNotNull(CurrentGroup);
    if (CurrentGroup.Name != name)
    {
      Log.Error(
        $"Trying to remove {name} group out of order. Groups must close in the order they were opened.");
      return;
    }
    if (!invalidName)
      DevLog.WriteVerbose($"-- End Group ({name})");
    CurrentGroup.Close();
    CurrentGroup = CurrentGroup.Parent;
  }

  public static void Cancel(string message = null)
  {
    Expect.SendSignal(Status.Canceled, "Test.Cancel", message, skipFrames: 2);
  }

  public static void Skip(string message = null)
  {
    Expect.SendSignal(Status.Skipped, "Test.Skip", message, skipFrames: 2);
  }

  public static IEnumerator Suspend(float secondsTimeOut, string message = null)
  {
    const int SecondsToMS = 1000;
    const int MaxTimeOut = 10 * 60 * SecondsToMS; // 10 minutes

    Assert.IsTrue(secondsTimeOut > 0);
    int maxTimeOut = Mathf.CeilToInt(MaxTimeOut);
    int countdownTime = Mathf.Min(Mathf.CeilToInt(secondsTimeOut), maxTimeOut);
    using CancellationTokenSource token = new(maxTimeOut);

    Dialog_TestSuspension dlg = new(message, countdownTime, token);
    Find.WindowStack.Add(dlg);
    while (!token.IsCancellationRequested)
      yield return null;
    Find.WindowStack.TryRemove(dlg);
  }

  public readonly struct Group : IDisposable
  {
    private readonly string label;

    public Group(string label)
    {
      this.label = label;
      BeginGroup(this.label);
    }

    public void Dispose()
    {
      EndGroup(label);
    }
  }
}