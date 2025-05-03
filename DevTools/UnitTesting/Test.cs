using System;
using System.Globalization;
using System.IO;
using DevTools.Benchmarking;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;

namespace DevTools.UnitTesting;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public static class Test
{
  // There should always be 1 group if testing is in progress, an empty one will be used
  // as the root test group of the test method.
  internal static ContextGroup CurrentGroup { get; private set; }

  private static readonly string logFilePath;
  private static StreamWriter writer;

  static Test()
  {
    logFilePath = Path.Combine(Application.persistentDataPath, "UnitTest.log");
  }

  public static void BeginGroup(string name)
  {
    bool invalidName = name.NullOrEmpty();
    // If CurrentGroup is null, we're opening a group for the root test
    if (invalidName && CurrentGroup != null)
    {
      // Must send to player.log before throwing since test exceptions are caught and logged to
      // the test log, and NOT the player.log, this is primarily for visibility and clear separation
      // from game logs, but this is a user-error that should be visible in the player log.
      Verse.Log.Error("Attempting to open empty Test.Group, this is not allowed.");
      throw new ArgumentException("Empty group name");
    }
    if (!invalidName)
      Log($"-- Begin Group ({name})");
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
      Verse.Log.Error(
        $"Trying to remove {name} group out of order. Groups must close in the order they were opened.");
      return;
    }
    if (!invalidName)
      Log($"-- End Group ({name})");
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

  public static void Suspend(string message = null)
  {
    //Expect.SendSignal(Status.Skipped, "Test.Skip", message, skipFrames: 2);
  }

  public static void Log(string message)
  {
    if (writer == null)
    {
      Verse.Log.Error(
        "Trying to log message to Test.Log while stream writer is closed.");
      return;
    }
    writer.WriteLine(message);
  }

  public static void OpenLogFile()
  {
    if (File.Exists(logFilePath))
    {
      Application.OpenURL(logFilePath);
    }
  }

  internal static string TimeLabel(double value)
  {
    return value < 1 ?
      $"< 1 {Benchmark.MeasurementSuffix(Benchmark.Measurement.Milliseconds)}" :
      $"{value:0} {Benchmark.MeasurementSuffix(Benchmark.Measurement.Milliseconds)}";
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

  internal readonly struct TestLogger : IDisposable
  {
    public TestLogger()
    {
      // Creates or clears log file, we can immediately close it since
      // we want to open with StreamWriter with append mode.
      File.Create(logFilePath).Close();
      writer = new StreamWriter(logFilePath, append: true);
      Log(
        $"{DateTime.Now.ToString("g", DateTimeFormatInfo.CurrentInfo)}{Environment.NewLine}{Environment.NewLine}");
    }

    public void Dispose()
    {
      writer.Dispose();
      writer = null;
    }
  }
}