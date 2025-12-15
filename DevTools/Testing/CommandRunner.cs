using DevTools.Testing;
using System;
using Verse;

namespace DevTools;

internal static class CommandRunner
{
	public static Result ExecuteCommandLineArgs(ModContentPack mod)
	{
		const string PackageIdArg = "--pid";

		const string RunTestsArg = "--test";
		const string FilterArg = "--where";

		const string RunPlanArg = "--test-plan";

		const string SmokeTestArg = "--smoke-test";

		const string BatchMode = "-batchmode";
		const string ExitAtEnd = "-e"; // Temporary solution since headless doesn't currently function with RimWorld
		const string NoGraphics = "-nographics";

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
				case BatchMode:
					result.headless = true;
					result.exitOnFinish = true;
				break;
				case ExitAtEnd:
					result.exitOnFinish = true;
				break;
				case NoGraphics:
					result.graphicsDevice = false;
				break;
			}
		}
		if (!result.packageId.EqualsIgnoreCase(mod.PackageIdPlayerFacing))
			return null;

		ExpressionTree expressionTree = null;
		if (!result.filterStr.NullOrEmpty())
		{
			expressionTree = ExpressionGenerator.Create(result.filterStr);
		}

		switch (testToRun)
		{
			case TestCommand.Plan:
			{
				DevHarmony.GetDevTool<TestPlanManager>(mod).Run(result.testPlan);
			}
			break;
			case TestCommand.Unit:
			{
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
				TestRunner testRunner =
					expressionTree != null ? testManager.GetRunnerWith(expressionTree) : new TestRunner(testManager);
				testRunner.Run();
			}
			break;
		}
		return result;
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

		public bool headless;
		public bool exitOnFinish;
		public bool graphicsDevice = true;
	}
}