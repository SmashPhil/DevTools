using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using Verse;

namespace DevTools.UnitTesting;

internal class LogWatcher : IDisposable
{
  private readonly UnitTestManager unitTestManager;
  private readonly Dictionary<LogType, List<string>> logCounts = [];

  public LogWatcher(UnitTestManager unitTestManager)
  {
    this.unitTestManager = unitTestManager;
    Application.logMessageReceivedThreaded += LogReceived;
  }

  public UnitTestManager UnitTestManager => unitTestManager;

  [MustUseReturnValue]
  public List<string> LogsOfType(LogType type)
  {
    return logCounts.TryGetValue(type, fallback: null);
  }

  public void Flush()
  {
    logCounts.Clear();
  }

  private void LogReceived(string msg, string stackTrace, LogType type)
  {
    if (!logCounts.ContainsKey(type))
      logCounts[type] = [];
    logCounts[type].Add(msg);
  }

  public void Dispose()
  {
    Application.logMessageReceivedThreaded -= LogReceived;
  }
}