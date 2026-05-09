using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace DevTools.Testing;

internal class DefaultSmokeTests
{
	private static readonly Dictionary<LogMessageType, List<LogWatcher.LogEntry>> LogMessagesOnStartup = [];

	// Hooks into unity log event for test validation when a runner starts
	private static LogWatcher logWatcher;

	[SmokeTest(TestType.MainMenu), ExecutionPriority(Priority.First)]
	private void VerifyStartupLogs()
	{
		if (TestRunner.Current?.testManager is not SmokeTestManager testManager)
			throw new InvalidOperationException("Invoking preset smoke test outside of SmokeTest runner.");

		CacheStartupLogs();

		SmokeTestConfig config = testManager.Config;
		logWatcher = new LogWatcher(config);

		foreach (LogWatcher.LogEntry logEntry in LogMessagesOnStartup[LogMessageType.Warning])
    {
      LogWatcher.TestLogEntry(config, LogType.Warning, logEntry);
		}
		foreach (LogWatcher.LogEntry logEntry in LogMessagesOnStartup[LogMessageType.Error])
		{
      LogWatcher.TestLogEntry(config, LogType.Error, logEntry);
    }
	}

	[SmokeTest(TestType.PostGameExit), ExecutionPriority(Priority.Last)]
	private void VerifyPostStartupLogs()
	{
		if (TestRunner.Current?.testManager is not SmokeTestManager)
			throw new InvalidOperationException("Invoking preset smoke test outside of SmokeTest runner.");

		logWatcher.Dispose();
		logWatcher = null;
	}

	private static void CacheStartupLogs()
	{
		if (!LogMessagesOnStartup.NullOrEmpty())
			return;

		PeekStartupLogs();
	}

	private static void PeekStartupLogs()
	{
		LogMessagesOnStartup[LogMessageType.Message] = [];
		LogMessagesOnStartup[LogMessageType.Warning] = [];
		LogMessagesOnStartup[LogMessageType.Error] = [];

		foreach (LogMessage message in Log.Messages)
		{
			LogMessagesOnStartup[message.type].Add(new LogWatcher.LogEntry(message.text, message.StackTrace));
		}
	}
}