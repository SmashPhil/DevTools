using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace DevTools.Testing;

/// <summary>
/// Simple smoke test for detecting startup errors and doing a quick runthrough of all defs added by this modlist
/// to verify that there are no immediate startup or runtime errors.
/// </summary>
internal class SmokeTestManager : IDevToolWithMenu, ITestManager
{
	private static readonly Dictionary<LogMessageType, List<string>> LogMessagesOnStartup = [];

	private readonly Dictionary<string, SmokeTestGroup> smokeTests = [];

	private SmokeTestConfig config;

	string IDevToolWithMenu.Name => "Smoke Test";

	string ITestManager.ConfigName => "SmokeTestConfig";

	public SmokeTestConfig Config => config;

	ITestConfig ITestManager.Config => config;

	public IEnumerable<ITestGroup> TestGroups => smokeTests.Values;

	private string LogTestKey => GetType().FullName;

	void IDevTool.Init(ModContentPack mod)
	{
		SmokeTestGroup smokeTestStartup = new(GetType(), TestType.MainMenu);
		smokeTestStartup.TryAddFunction(AccessTools.Method(typeof(SmokeTestManager), nameof(VerifyStartupLogs)));

		smokeTests[LogTestKey] = smokeTestStartup;

		PeekStartupLogs();

		config = this.LoadConfig<SmokeTestConfig>(mod);
	}

	bool IDevTool.TryRegisterType(Type type)
	{
		bool anyAdded = false;
		foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
		{
			if (method.TryGetAttribute<SmokeTestAttribute>() is not { } smokeTestAttr)
				continue;

			string key = type.FullName;
			if (key == null)
				return false;

			bool added = !smokeTests.TryGetValue(key, out SmokeTestGroup testGroup);
			testGroup ??= new SmokeTestGroup(type, smokeTestAttr.Type);
			testGroup.MetaData.Load(type);

			if (testGroup.MetaData.Get<bool>(MetaDataName.Disabled))
				continue;

			if (added)
			{
				smokeTests[key] = testGroup;
			}
			anyAdded |= testGroup.TryAddFunction(method);
		}
		return anyAdded;
	}

	void ITestManager.OnTestRunnerEnd()
	{
		const string HeadlessArg = "-batchmode";

		if (Environment.GetCommandLineArgs().Contains(HeadlessArg))
		{
			bool anyfailed = TestGroups.Any(group => group.Status == Status.Failed);
			Application.Quit(anyfailed ? 1 : 0);
			return;
		}
		OpenMenu();
	}

	public void RunAll()
	{
		new TestRunner(this).Run();
	}

	// If smoke test is launched without batch mode, the results need to be shown somewhere. This menu is otherwise
	// inaccessible in-game as there is no reason to launch a 'smoke test' from main menu yet.
	// TODO - Add def spawning as an option for smoke tests (ie. the default for menu-launched smoke tests)
	public void OpenMenu()
	{
		Find.WindowStack.Add(new Dialog_TestExplorer(this));
	}

	[SmokeTest(TestType.MainMenu), ExecutionPriority(Priority.First)]
	private static void VerifyStartupLogs()
	{
		if (TestRunner.Current?.testManager is not SmokeTestManager testManager)
			throw new InvalidOperationException("Invoking preset smoke test outside of SmokeTest runner.");

		SmokeTestConfig config = testManager.Config;

		ITestGroup mainMenuGroup = testManager.smokeTests[testManager.LogTestKey];
		foreach (string message in LogMessagesOnStartup[LogMessageType.Warning])
		{
			if (!LogWatcher.LogAllowed(config, LogType.Warning, message, out string failReason))
			{
				mainMenuGroup.Fail(failReason);
				return;
			}
		}
		foreach (string message in LogMessagesOnStartup[LogMessageType.Error])
		{
			if (!LogWatcher.LogAllowed(config, LogType.Error, message, out string failReason))
			{
				mainMenuGroup.Fail(failReason);
				return;
			}
		}
	}

	private static void PeekStartupLogs()
	{
		LogMessagesOnStartup[LogMessageType.Message] = [];
		LogMessagesOnStartup[LogMessageType.Warning] = [];
		LogMessagesOnStartup[LogMessageType.Error] = [];

		foreach (LogMessage message in Log.Messages)
		{
			LogMessagesOnStartup[message.type].Add(message.text);
		}
	}
}