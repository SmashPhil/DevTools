using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

namespace DevTools.Testing;

/// <summary>
/// Test Manager for running unit tests in RimWorld. Tests can be ran in isolation or be executed
/// as part of a test suite. This manager will handle switching between scenes and consolidating
/// test results in an explorer widget, allowing you to view each test class and its results.
/// <para/>
/// Due to Unity being single threaded, tests are run synchronously. This will block the
/// main thread and cause the application to hang for the duration of test execution. Because
/// RimWorld is so tightly coupled it's impossible to predict where it might call to Unity's API.
/// </summary>
[PublicAPI]
public class UnitTestManager : IDevToolWithMenu, ITestManager
{
	private const string ManagerName = "Unit Test";

	// Debugging only
	internal static readonly bool BreakOnTestFailure;

	private ModContentPack mod;
	private UnitTestConfig config;
	private readonly Dictionary<string, UnitTestGroup> unitTests = [];
	private readonly Dialog_TestExplorer testExplorer;

	public UnitTestManager()
	{
		testExplorer = new Dialog_TestExplorer(this, new Dialog_TestExplorer.TestExplorerEntryComparer());
	}

	string ITestManager.ConfigName => "UnitTestConfig";

	ITestConfig ITestManager.Config => config;

	public UnitTestConfig Config => config;

	internal IEnumerable<UnitTestGroup> UnitTests => unitTests.Values;

	IEnumerable<ITestGroup> ITestManager.TestGroups => unitTests.Values;

	string IDevToolWithMenu.Name => ManagerName;

	public void OpenMenu()
	{
		Find.WindowStack.Add(testExplorer);
	}

	bool IDevTool.TryRegisterType(Type type)
	{
    TestClassAttribute attr = type.TryGetAttribute<TestClassAttribute>();
		if (attr is null || type.IsAbstract)
			return false;
		string key = type.FullName;
		if (key == null)
			return false;
		if (!type.HasAllRequiredMods() || !type.HasAnyRequiredMods())
			return false;

		UnitTestGroup testGroup = new(this, type, attr.Type);
		testGroup.MetaData.Load(type);
		if (testGroup.MetaData.Get<bool>(MetaDataName.Disabled))
			return false;
		unitTests.TryAdd(key, testGroup);
		testGroup.AddFromType(type);
		return true;
	}

	bool IDevTool.Init(ModContentPack mod)
	{
		this.mod = mod;
		foreach (UnitTestGroup testGroup in unitTests.Values)
		{
			if (testGroup.TestCount == 0)
				Log.Warning($"{testGroup.Name} has 0 tests. Execution will be skipped.");
		}
		foreach (UnitTestGroup testGroup in unitTests.Values)
		{
			testGroup.SortByExecutionPriority();
		}
		config = this.LoadConfig<UnitTestConfig>(mod);
		return config != null;
	}

	internal bool TryGetUnitTest(string fullName, out UnitTestGroup testGroup)
	{
		return unitTests.TryGetValue(fullName, out testGroup);
	}

	void ITestManager.OnTestRunnerStart()
	{
	}

	void ITestManager.OnTestRunnerEnd()
	{
		if (DevHarmony.Args is { exitOnFinish: true })
		{
			bool anyFailed = UnitTests.Any(group => group.Status == Status.Failed);
			DevLog.Write($"Test runner finished. Result: {(anyFailed ? "Failed" : "Passed")}");
			Application.Quit(anyFailed ? 1 : 0);
			return;
		}
		OpenMenu();
	}

	public void OpenLogFile()
	{
		if (!File.Exists(config.log.FullPath))
		{
			Messages.Message("No log file to open.", MessageTypeDefOf.RejectInput);
			return;
		}
		Application.OpenURL(config.log.FullPath);
	}

	public void RunAll()
	{
		TestRunner runner = new(this);
		runner.AddTestActions(Config);
		runner.Run();
	}

	private static void TestExceptionHandler(Exception ex)
	{
		DelayedErrorWindowRequest.Add($"Exception thrown while running tests.\n{ex}",
			"UnitTestManager Aborted Operation");
		Scribe.ForceStop();
		GenScene.GoToMainMenu();
	}
}