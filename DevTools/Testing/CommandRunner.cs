using System;
using System.Collections.Generic;
using System.IO;
using DevTools.Testing;
using Verse;

namespace DevTools;

internal static class CommandRunner
{
	private static readonly List<TestPlan> TestPlans = [];

	public static Result ExecuteCommandLineArgs(ModContentPack mod)
	{
		const string PackageIdArg = "--pid";

		const string RunTestsArg = "--test";
		const string FilterArg = "--where";
		const string RunPlanArg = "--plan";

		const string SmokeTestArg = "--smoke-test";

		TestCommand testToRun = TestCommand.None;
		string[] args = Environment.GetCommandLineArgs();
		if (args.Length == 0)
			return null;

		Result result = new();
		for (int i = 0; i < args.Length; i++)
		{
			string arg = args[i];
			switch (arg)
			{
				case PackageIdArg:
					if (i + 1 < args.Length)
						result.packageId = args[++i];
				break;
				case RunTestsArg:
					testToRun = TestCommand.Unit;
				break;
				case SmokeTestArg:
					testToRun = TestCommand.Smoke;
				break;
				case RunPlanArg:
					testToRun = TestCommand.Plan;
					if (i + 1 < args.Length)
					{
						result.testPlan = args[++i];
					}
				break;
				case FilterArg:
					if (i + 1 < args.Length)
						result.filterStr = args[++i];
				break;
			}
		}
		if (!result.packageId.EqualsIgnoreCase(mod.PackageIdPlayerFacing))
			return null;

		switch (testToRun)
		{
			case TestCommand.Plan:
				ReloadTestPlans(mod);
			break;
			case TestCommand.Unit:
			{
				ExpressionTree expressionTree = null;
				if (!result.filterStr.NullOrEmpty())
				{
					expressionTree = ExpressionGenerator.Create(result.filterStr);
				}
				UnitTestManager testManager = DevHarmony.GetDevTool<UnitTestManager>(mod);
				TestRunner testRunner =
					expressionTree != null ? testManager.GetRunnerWith(expressionTree) : new TestRunner(testManager);
				testRunner.AddTestActions(testManager.Config);
				testRunner.Run();
			}
			break;
			case TestCommand.Smoke:
			{
				SmokeTestManager testManager = DevHarmony.GetDevTool<SmokeTestManager>(mod);
				TestRunner testRunner = new(testManager);
				testRunner.Run();
			}
			break;
		}
		return result;
	}

	private static void ReloadTestPlans(ModContentPack mod)
	{
		DirectoryInfo dirInfo = new(GenFile.ResolveCaseInsensitiveFilePath(mod.RootDir, "TestPlans"));
		if (dirInfo.Exists)
		{
			foreach (FileInfo file in dirInfo.GetFiles("*.xml", SearchOption.AllDirectories))
			{
				try
				{
					TestPlan testPlan = DirectXmlLoader.ItemFromXmlFile<TestPlan>(file.FullName);
					TestPlans.Add(testPlan);
				}
				catch (Exception ex)
				{
					Log.Error($"Exception thrown loading TestPlan {file.FullName}.\n{ex}");
				}
			}
		}
	}

	private enum TestCommand
	{
		None,
		Plan,
		Unit,
		Smoke
	}

	public record Result
	{
		public string packageId;

		// UnitTesting
		public string filterStr;
		public string testPlan;
	}
}