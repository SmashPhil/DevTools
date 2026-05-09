using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using Verse;

namespace DevTools.Testing;

internal class LogWatcher : IDisposable
{
  private static readonly ConcurrentDictionary<LogType, List<LogEntry>> LogCounts = [];

  private readonly ITestConfig config;

  public LogWatcher(ITestConfig config)
  {
    this.config = config;
    Application.logMessageReceivedThreaded += LogReceived;
  }

  [MustUseReturnValue]
  private static List<LogEntry> LogsOfType(LogType type)
  {
    return LogCounts.TryGetValue(type, fallback: null);
  }

  private static void LogReceived(string msg, string stackTrace, LogType type)
  {
    if (!LogCounts.ContainsKey(type))
    {
      LogCounts[type] = [];
    }
    LogCounts[type].Add(new LogEntry(msg, stackTrace));
  }

  public void Dispose()
  {
    try
    {
      VerifyLogs(LogType.Warning);
      VerifyLogs(LogType.Error);
      LogCounts.Clear();
    }
    finally
    {
      Application.logMessageReceivedThreaded -= LogReceived;
    }
  }

  private void VerifyLogs(LogType logType)
  {
    if (!config.VerifyForLogType(logType))
      return;

    ITestGroup current = Test.Current;
    if (current is { Status: Status.Failed })
      return;

    List<LogEntry> logs = LogsOfType(logType);
    if (logs.NullOrEmpty())
      return;

    foreach (LogEntry logEntry in logs)
    {
      TestLogEntry(config, logType, logEntry);
    }
  }

  public static void TestLogEntry(ITestConfig config, LogType logType, in LogEntry logEntry)
  {
    if (config.LogContained(logType, logEntry.message))
      return;

    switch (logType)
    {
      case LogType.Error or LogType.Assert or LogType.Exception:
        Test.Fail("Logged error not whitelisted for tests.", $"{logEntry.message}\"{Environment.NewLine}{logEntry.stackTrace}");
        break;
      case LogType.Warning:
        Test.Fail("Logged warning not whitelisted for tests.", $"{logEntry.message}\"{Environment.NewLine}{logEntry.stackTrace}");
        break;
    }
  }

  public readonly struct LogEntry(string message, string stackTrace)
  {
    public readonly string message = message;
    public readonly string stackTrace = stackTrace;
  }
}