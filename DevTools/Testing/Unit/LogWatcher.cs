using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using Verse;

namespace DevTools.Testing;

internal readonly struct LogWatcher : IDisposable
{
	private static readonly ConcurrentDictionary<LogType, List<LogEntry>> LogCounts = [];

	private readonly ITestConfig config;
	private readonly ITestCase testCase;

	public LogWatcher(ITestConfig config, ITestCase testCase = null)
	{
		this.config = config;
		this.testCase = testCase;
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
			LogCounts[type] = [];
		LogCounts[type].Add(new LogEntry(msg, stackTrace));
	}

	void IDisposable.Dispose()
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
		if (testCase is { Status: Status.Failed })
			return;

		List<LogEntry> logs = LogsOfType(logType);
		if (logs.NullOrEmpty())
			return;

		foreach (LogEntry entry in logs)
		{
			if (!LogAllowed(config, logType, entry.message, out string failReason))
			{
				testCase.Fail(failReason);
				return;
			}
		}
	}

	public static bool LogAllowed(ITestConfig config, LogType logType, string message, out string failReason)
	{
		failReason = null;
		if (config.LogContained(logType, message))
			return true;

		failReason = FailReason(logType, message);
		DevLog.Write($"{Expect.FailedLabel} {failReason}");
		return false;

		static string FailReason(LogType logType, string message)
		{
			return logType switch
			{
				LogType.Error or LogType.Assert or LogType.Exception =>
					$"Logged error not whitelisted for tests.\nError = \"{message}\"",
				LogType.Warning =>
					$"Logged warning not whitelisted for tests.\nWarning = \"{message}\"",
				_ => throw new NotImplementedException(nameof(LogType))
			};
		}
	}

	public readonly struct LogEntry(string message, string stackTrace)
	{
		public readonly string message = message;
		public readonly string stackTrace = stackTrace;
	}
}