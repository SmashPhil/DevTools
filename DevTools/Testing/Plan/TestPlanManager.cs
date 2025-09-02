using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using Verse;

namespace DevTools.Testing;

internal class TestPlanManager : IDevToolWithMenu, ITestManager
{
	private readonly List<string> modListToRestore = [];

	private List<TestPlan> testPlans = [];

	private Dialog_TestExplorer testExplorer;

	private TestPlan SelectedPlan { get; set; }

	string IDevToolWithMenu.Name => "Test Plan";

	string ITestManager.ConfigName => "TestPlanConfig";

	ITestConfig ITestManager.Config => SelectedPlan?.config;

	public IEnumerable<ITestGroup> TestGroups
	{
		get
		{
			if (SelectedPlan == null)
				yield break;

			foreach (TestPlanJob job in SelectedPlan.jobs)
			{
				yield return job;
			}
		}
	}

	bool IDevTool.Init(ModContentPack mod)
	{
		testPlans = LoadTestPlans(mod);
		testExplorer = new Dialog_TestExplorer(this);
		return !testPlans.NullOrEmpty();
	}

	bool IDevTool.TryRegisterType(Type type)
	{
		return true;
	}

	private static List<TestPlan> LoadTestPlans(ModContentPack mod)
	{
		const string TestPlanFolder = "TestPlans";

		DirectoryInfo dirInfo = new(GenFile.ResolveCaseInsensitiveFilePath(mod.RootDir, TestPlanFolder));
		if (!dirInfo.Exists)
			return null;

		List<TestPlan> plans = [];
		foreach (FileInfo file in dirInfo.GetFiles("*.xml", SearchOption.AllDirectories))
		{
			try
			{
				TestPlan testPlan = DirectXmlLoader.ItemFromXmlFile<TestPlan>(file.FullName);
				if (testPlan.IsValid)
				{
					testPlan.PostLoadInit(mod);
					plans.Add(testPlan);
				}
			}
			catch (Exception ex)
			{
				Log.Error($"Exception thrown loading TestPlan {file.FullName}.\n{ex}");
			}
		}
		return plans;
	}

	void ITestManager.OnTestRunnerStart()
	{
		modListToRestore.Clear();
		foreach (ModMetaData modMetaData in ModsConfig.ActiveModsInLoadOrder)
		{
			modListToRestore.Add(modMetaData.PackageIdNonUnique.ToLowerInvariant());
		}
	}

	void ITestManager.OnTestRunnerEnd()
	{
		ModsConfig.SaveFromList(modListToRestore);
		modListToRestore.Clear();

		if (DevHarmony.Args is { exitOnFinish: true })
		{
			bool anyFailed = TestGroups.Any(group => group.Status == Status.Failed);
			DevLog.Write($"Test runner finished. Result: {(anyFailed ? "Failed" : "Passed")}");
			Application.Quit(anyFailed ? 1 : 0);
			return;
		}
		OpenMenu();
	}

	[PublicAPI]
	public void Run(string name)
	{
		TestPlan plan = testPlans.FirstOrDefault(testPlan => testPlan.name.EqualsIgnoreCase(name));
		if (plan is null)
		{
			Log.Error($"Unable to locate TestPlan named {name}");
			return;
		}
		Run(plan);
	}

	[PublicAPI]
	public void Run(TestPlan testPlan)
	{
		SelectedPlan = testPlan;
		testExplorer.Refresh();
		new TestRunner(this, TestFilter).Run();
		LongEventHandler.ExecuteWhenFinished(delegate { CoroutineObject.Instance.StartCoroutine(OpenMenuRoutine()); });
	}

	[PublicAPI]
	public void RunAll()
	{
		List<FloatMenuOption> options = [];
		foreach (TestPlan plan in testPlans)
		{
			options.Add(new FloatMenuOption(plan.name, delegate { Run(plan); }));
		}
		Find.WindowStack.Add(new FloatMenu(options));
	}

	private static IEnumerable<(ITestGroup, List<ITestFunction>)> TestFilter(ITestManager testManager)
	{
		if (testManager is not TestPlanManager testPlanManager)
			throw new InvalidOperationException("Using TestPlan filter for non test plan runner.");

		foreach (TestPlanJob job in testPlanManager.SelectedPlan.jobs)
		{
			yield return (job, [.. job.TestFunctions]);
		}
	}

	private IEnumerator OpenMenuRoutine()
	{
		while (Current.ProgramState is not ProgramState.Entry || Find.WindowStack is null)
			yield return null;

		OpenMenu();
	}

	public void OpenMenu()
	{
		Find.WindowStack.Add(testExplorer);
	}
}