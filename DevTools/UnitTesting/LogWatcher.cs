using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using Verse;

namespace DevTools.UnitTesting;

internal readonly struct LogWatcher : IDisposable
{
  private static readonly ConcurrentDictionary<LogType, List<LogEntry>> logCounts = [];

  private readonly ITestCase testCase;

  public LogWatcher(ITestCase testCase)
  {
    this.testCase = testCase;
    Application.logMessageReceivedThreaded += LogReceived;
  }

  [MustUseReturnValue]
  public static List<LogEntry> LogsOfType(LogType type)
  {
    return logCounts.TryGetValue(type, fallback: null);
  }

  private static void LogReceived(string msg, string stackTrace, LogType type)
  {
    if (!logCounts.ContainsKey(type))
      logCounts[type] = [];
    logCounts[type].Add(new LogEntry(msg, stackTrace));
  }

  void IDisposable.Dispose()
  {
    try
    {
      testCase.VerifyLogs(LogType.Warning);
      testCase.VerifyLogs(LogType.Error);
      logCounts.Clear();
    }
    finally
    {
      Application.logMessageReceivedThreaded -= LogReceived;
    }
  }

  public readonly struct LogEntry(string message, string stackTrace)
  {
    public readonly string message = message;
    public readonly string stackTrace = stackTrace;
  }
}