using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using UnityEngine;
using Verse;

namespace DevTools.Testing;

internal class LogWatcher : IDisposable
{
  private const string IdeoPackageId = "Ludeon.RimWorld.Ideology";

  private static readonly ConcurrentDictionary<LogType, List<LogEntry>> LogCounts = [];

  private static readonly (LogType logType, string packageId, string pattern)[] VanillaLogsAllowed =
  [
    // Ideo.ExposeData adds Funeral (no corpse) to non-classic ideoligions on load
    (LogType.Warning, IdeoPackageId, "^A hidden ritual precept was missing, adding: (.+)$"),
    // Relations-related bug where transient pawn leaves relations with colonists, resulting in missing pawn refs.
    (LogType.Warning, packageId: null,
      @"Object with load ID Thing_[^\s]+ is referenced \(xml node name: .+?\) but is not deep-saved\. This will cause errors during loading\."),
    // GenStep_BaseRuins with small maps fails to find valid location, but this fails safely.
    (LogType.Warning, packageId: null, "did not find any valid rects to generate ruins in for bounds"),
    // If ideo is removed while still belonging to a faction. Occurs sporadically on subsequent test runs.
    (LogType.Error, packageId: IdeoPackageId, "^Faction .+? contains ideo .+? which was removed!$")
  ];

  private readonly ITestConfig config;
  private readonly ITestCase[] scopes;

  /// <param name="config">Config for verifying log allowance before failing a test.</param>
  /// <param name="scopes">
  /// Tests whose <see cref="WarningsAllowedAttribute"/>, <see cref="ErrorsAllowedAttribute"/>,
  /// <see cref="WarningsAllowedFromAttribute"/> and <see cref="ErrorsAllowedFromAttribute"/> apply to logs
  /// captured by this watcher.
  /// </param>
  public LogWatcher(ITestConfig config, params ITestCase[] scopes)
  {
    this.config = config;
    this.scopes = scopes;
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
      TestLogEntry(config, logType, logEntry, scopes);
    }
  }

  public static void TestLogEntry(
    ITestConfig config,
    LogType logType,
    in LogEntry logEntry,
    params ITestCase[] scopes
  )
  {
    if (
      VanillaLogAllowed(logType, logEntry.message)
      || config.LogContained(logType, logEntry.message)
      || AllowedByScope(scopes, logType, logEntry.message, logEntry.stackTrace)
    )
      return;

    switch (logType)
    {
      case LogType.Error or LogType.Assert or LogType.Exception:
        Test.Fail(
          "Logged error not whitelisted for tests.",
          failureMessage: $"\"{logEntry.message}\"{Environment.NewLine}{logEntry.stackTrace}"
        );
        break;
      case LogType.Warning:
        Test.Fail(
          "Logged warning not whitelisted for tests.",
          failureMessage: $"\"{logEntry.message}\"{Environment.NewLine}{logEntry.stackTrace}"
        );
        break;
    }
  }

  internal static bool VanillaLogAllowed(LogType logType, string message)
  {
    foreach ((LogType type, string packageId, string pattern) in VanillaLogsAllowed)
    {
      if (type != logType)
        continue;
      if (!packageId.NullOrEmpty() && ModLister.GetActiveModWithIdentifier(packageId, ignorePostfix: true) is null)
        continue;

      if (Regex.IsMatch(message, pattern))
        return true;
    }
    return false;
  }

  internal static bool AllowedByScope(ITestCase[] scopes, LogType logType, string message, string stackTrace)
  {
    string key =
      logType is LogType.Warning ? MetaDataName.WarningsAllowed : MetaDataName.ErrorsAllowed;
    string fromKey =
      logType is LogType.Warning ? MetaDataName.WarningsAllowedFrom : MetaDataName.ErrorsAllowedFrom;
    foreach (ITestCase scope in scopes)
    {
      if (AnyMatch(scope.MetaData.Get<string[]>(key), message))
        return true;

      // A stack pattern that matches an empty string matches every stack, so it would allow the message from anywhere.
      if (scope.MetaData.Get<StackAllowance>(fromKey) is { StackPattern: { } stackPattern } allowance
        && AnyMatch(allowance.Patterns, message)
        && !Regex.IsMatch(string.Empty, stackPattern) && Regex.IsMatch(stackTrace ?? string.Empty, stackPattern))
        return true;
    }
    return false;
  }

  private static bool AnyMatch(string[] patterns, string input)
  {
    if (patterns is null)
      return false;
    foreach (string pattern in patterns)
    {
      if (Regex.IsMatch(input, pattern))
        return true;
    }
    return false;
  }

  public readonly struct LogEntry(string message, string stackTrace)
  {
    public readonly string message = message;
    public readonly string stackTrace = stackTrace;
  }
}
