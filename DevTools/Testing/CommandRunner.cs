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
    const string FilterArg = "--filter";
    const string WhereArg = "--where";

    const string RunPlanArg = "--test-plan";
    const string SmokeTestArg = "--smoke-test";

    const string RunTestsShort = "-t";
    const string FilterShort = "-f";
    const string RunPlanShort = "-p";
    const string SmokeTestShort = "-s";

    const string BatchMode = "-batchmode";
    const string NoGraphics = "-nographics";
    // Temporary solution since headless doesn't currently function with RimWorld
    const string ExitAtEndArg = "--exit";
    const string ExitAtEndShort = "-e";

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
        case RunTestsArg or RunTestsShort:
          testToRun = TestCommand.Unit;
          break;
        case SmokeTestArg or SmokeTestShort:
          testToRun = TestCommand.Smoke;
          break;
        case RunPlanArg or RunPlanShort:
          testToRun = TestCommand.Plan;
          if (i + 1 < args.Length)
          {
            result.testPlan = args[++i];
          }
          break;
        case FilterArg or WhereArg or FilterShort:
          if (i + 1 < args.Length)
            result.filterStr = args[++i];
          break;
        case ExitAtEndArg or ExitAtEndShort:
          result.exitOnFinish = true;
          break;
        case BatchMode:
          result.headless = true;
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
          TestFixtureManager testManager = DevHarmony.GetDevTool<TestFixtureManager>(mod);
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

    // Testing
    public string filterStr;
    public string testPlan;

    public bool headless;
    public bool exitOnFinish;
    public bool graphicsDevice = true;
  }
}