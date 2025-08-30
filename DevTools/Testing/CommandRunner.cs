using System;
using System.Collections.Generic;
using System.IO;
using DevTools.UnitTesting;
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

		bool runTests = false;
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
					runTests = true;
				break;
				case RunPlanArg:
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
		if (result.packageId != mod.PackageIdPlayerFacing)
			return null;

		if (runTests)
		{
			if (result.testPlan != null)
			{
				ReloadTestPlans(mod);
			}

			ExpressionTree expressionTree = null;
			if (!result.filterStr.NullOrEmpty())
			{
				expressionTree = ExpressionGenerator.Create(result.filterStr);
			}
			UnitTestManager testManager = DevHarmony.GetDevTool<UnitTestManager>(mod);
			TestRunner testRunner =
				expressionTree != null ? testManager.GetRunnerWith(expressionTree) : new TestRunner(testManager);
			testRunner.Run();
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

	public record Result
	{
		public string packageId;

		// UnitTesting
		public string filterStr;
		public string testPlan;
	}
}