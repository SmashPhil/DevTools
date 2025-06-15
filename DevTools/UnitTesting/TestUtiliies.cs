using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;

namespace DevTools.UnitTesting;

internal static class TestUtiliies
{
  public static bool IsSubRoutine(this ITestFunction testFunction)
  {
    return testFunction.MethodInfo.ReturnType == typeof(IEnumerator);
  }

  public static bool IsDisabled(this ITestCase testCase)
  {
    return testCase.MetaData.Get<bool>(MetaDataName.Disabled);
  }

  public static void VerifyLogs(this ITestCase testCase, LogType logType)
  {
    UnitTestManager unitTestManager = UnitTestManager.CurrentActive;
    Assert.IsNotNull(unitTestManager);
    TestConfig config = unitTestManager.Config;
    Assert.IsNotNull(config);
    if (!config.VerifyForLogType(logType) || testCase.Status == Status.Failed)
      return;

    List<LogWatcher.LogEntry> logs = LogWatcher.LogsOfType(logType);
    if (logs.NullOrEmpty())
      return;

    foreach (LogWatcher.LogEntry entry in logs)
    {
      if (!config.LogContained(logType, entry.message))
      {
        string reason = FailReason(logType, entry);
        DevLog.Write($"{Expect.FailedLabel} {reason}");
        testCase.Fail(reason);
        return;
      }
    }
    return;

    static string FailReason(LogType logType, in LogWatcher.LogEntry entry)
    {
      return logType switch
      {
        LogType.Error or LogType.Assert or LogType.Exception =>
          $"Logged error not whitelisted for tests.\nError = \"{entry.message}\"{Environment.NewLine}{entry.stackTrace}",
        LogType.Warning =>
          $"Logged warning not whitelisted for tests.\nWarning = \"{entry.message}\"{Environment.NewLine}{entry.stackTrace}",
        _ => throw new NotImplementedException(nameof(LogType))
      };
    }
  }
}