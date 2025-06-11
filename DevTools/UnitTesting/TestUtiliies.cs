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

  public static void VerifyAllLogsAndFlush(this ITestCase testCase, LogWatcher watcher)
  {
    testCase.VerifyLogs(watcher, LogType.Warning);
    testCase.VerifyLogs(watcher, LogType.Error);
    // Clears results for next test case
    watcher.Flush();
  }

  private static void VerifyLogs(this ITestCase testCase, LogWatcher watcher, LogType logType)
  {
    TestConfig config = watcher.UnitTestManager.Config;
    Assert.IsNotNull(config);
    if (!config.VerifyForLogType(logType) || testCase.Status == Status.Failed)
      return;

    List<string> logs = watcher.LogsOfType(logType);
    if (logs.NullOrEmpty())
      return;

    foreach (string message in logs)
    {
      if (!config.LogContained(logType, message))
      {
        string reason = FailReason(logType, message);
        DevLog.Write($"{Expect.FailedLabel} {reason}");
        testCase.Fail(reason);
        return;
      }
    }
    return;

    static string FailReason(LogType logType, string message)
    {
      return logType switch
      {
        LogType.Error or LogType.Assert or LogType.Exception =>
          $"Logged error not whitelisted for tests.\nError = \"{message}\"",
        LogType.Warning => $"Logged warning not whitelisted for tests.\nWarning = \"{message}\"",
        _               => throw new NotImplementedException(nameof(LogType))
      };
    }
  }
}