using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;

namespace DevTools.Testing;

/// <summary>
/// Simple smoke test for detecting startup errors and doing a quick runthrough of all defs added by this modlist
/// to verify that there are no immediate startup or runtime errors.
/// </summary>
internal class SmokeTestManager : IDevToolWithMenu, ITestManager
{
	private static readonly string DefaultTestKey = typeof(DefaultSmokeTests).FullName;

	private readonly Dictionary<(string, TestType), SmokeTestGroup> smokeTests = [];

	private SmokeTestConfig config;

	string IDevToolWithMenu.Name => "Smoke Test";

	string ITestManager.ConfigName => "SmokeTestConfig";

	public SmokeTestConfig Config => config;

	ITestConfig ITestManager.Config => config;

	public IEnumerable<ITestGroup> TestGroups => smokeTests.Values;

	bool IDevTool.Init(ModContentPack mod)
	{
		config = this.LoadConfig<SmokeTestConfig>(mod);
		return config != null;
	}

	bool IDevTool.TryRegisterType(Type type)
	{
		bool anyAdded = false;
		foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static |
			BindingFlags.Instance))
		{
			if (method.TryGetAttribute<SmokeTestAttribute>() is not { } smokeTestAttr)
				continue;

			string key = type.FullName;
			if (key == null)
				return false;

			bool added = !smokeTests.TryGetValue((key, smokeTestAttr.Type), out SmokeTestGroup testGroup);
			testGroup ??= new SmokeTestGroup(type, smokeTestAttr.Type);
			testGroup.MetaData.Load(type);

			if (testGroup.MetaData.Get<bool>(MetaDataName.Disabled))
				continue;

			if (added)
			{
				smokeTests[(key, smokeTestAttr.Type)] = testGroup;
			}
			anyAdded |= testGroup.TryAddFunction(method);
		}
		return anyAdded;
	}

	void ITestManager.OnTestRunnerStart()
	{
	}

	void ITestManager.OnTestRunnerEnd()
	{
		if (DevHarmony.Args is { exitOnFinish: true })
		{
			bool anyFailed = TestGroups.Any(group => group.Status == Status.Failed);
			DevLog.Write($"Test runner finished. Result: {(anyFailed ? "Failed" : "Passed")}");
			Application.Quit(anyFailed ? 1 : 0);
			return;
		}
		OpenMenu();
	}

	internal ITestGroup GetDefaultGroup(TestType testType)
	{
		return smokeTests.TryGetValue((DefaultTestKey, testType));
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
		Find.WindowStack.Add(new Dialog_TestExplorer(this, new Dialog_TestExplorer.TestExplorerEntryComparer()));
	}
}