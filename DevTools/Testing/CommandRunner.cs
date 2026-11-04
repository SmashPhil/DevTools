using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DevTools.Testing;
using Verse;

namespace DevTools;

internal static class CommandRunner
{
  private const string PackageIdArg = "--pid";

  private const string RunTestsArg = "--test";
  private const string FilterArg = "--filter";
  private const string WhereArg = "--where";

  private const string RunPlanArg = "--test-plan";
  private const string RunListArg = "--test-file";
  private const string SmokeTestArg = "--smoke-test";

  private const string RunTestsShort = "-t";
  private const string WhereShort = "-w";
  private const string RunPlanShort = "-p";
  private const string SmokeTestShort = "-s";

  private const string ExitAtEndArg = "--exit";
  private const string ExitAtEndShort = "-e";
  private const string PortArg = "--port";
  private const string SessionArg = "--session";

  // Waylith
  private const string NoWindow = "-NoWindow";
  private const string Headless = "-Headless";

  public static Args ProcessCommandLineArgs()
  {
    string[] argsStr = Environment.GetCommandLineArgs();
    if (argsStr is not { Length: > 0 })
      return null;

    Args args = new();
    for (int i = 0; i < argsStr.Length; i++)
    {
      string arg = argsStr[i];
      switch (arg)
      {
        case PackageIdArg:
          if (i + 1 < argsStr.Length)
            args.packageIds = argsStr[++i].Split(';');
          break;
        case RunTestsArg or RunTestsShort:
          args.ManagerType = ManagerType.Unit;
          break;
        case SmokeTestArg or SmokeTestShort:
          args.ManagerType = ManagerType.Smoke;
          break;
        case RunPlanArg or RunPlanShort:
          args.ManagerType = ManagerType.Plan;
          if (i + 1 < argsStr.Length)
          {
            args.TestPlan = argsStr[++i];
          }
          break;
        case RunListArg:
          args.TestListFile = argsStr[++i];
          break;
        case WhereArg or WhereShort or FilterArg:
          if (i + 1 < argsStr.Length)
            args.Expression = argsStr[++i];
          break;
        case ExitAtEndArg or ExitAtEndShort:
          args.exitOnFinish = true;
          break;
        case NoWindow:
          args.hideWindow = true;
          args.exitOnFinish = true;
          break;
        case Headless:
          args.headless = true;
          break;
        case PortArg:
          string portStr = argsStr[++i];
          if (!int.TryParse(portStr, out int port))
          {
            Console.Error.WriteLine($"Unable to parse port {portStr}");
          }
          else
          {
            args.port = port;
          }
          break;
        case SessionArg:
          args.sessionId = argsStr[++i];
          break;
      }
    }
    return args;
  }

  public static TestRunner CreateRunner(ModContentPack mod, Args args)
  {
    if (!args.packageIds.Any(str => str.EqualsIgnoreCase(mod.PackageIdPlayerFacing)))
      return null;

    ITestManager testManager = args.ManagerType switch
    {
      ManagerType.Unit => DevHarmony.GetDevTool<TestFixtureManager>(mod),
      ManagerType.Smoke => DevHarmony.GetDevTool<SmokeTestManager>(mod),
      ManagerType.Plan => DevHarmony.GetDevTool<TestPlanManager>(mod),
      _ => throw new NotImplementedException(args.ManagerType.ToString())
    };
    if (testManager == null)
    {
      string tests = args.ManagerType switch
      {
        ManagerType.Smoke => "smoke tests",
        ManagerType.Plan => "test plans",
        _ => "unit tests"
      };
      Log.Error($"{mod.PackageIdPlayerFacing} has no {tests}.");
      DevApplication.Exit(ExitCode.NoTests);
      return null;
    }
    if (args.ManagerType is ManagerType.Plan && args.TestPlan == null)
    {
      Log.Error(args.TestPlan != null
        ? $"{mod.PackageIdPlayerFacing} has no test plan named {args.TestPlan}."
        : $"{RunPlanArg} needs the name of a plan.");
      DevApplication.Exit(ExitCode.NoTests);
      return null;
    }

    TestRunner testRunner = new(testManager);
    if (!args.Expression.NullOrEmpty())
    {
      testRunner.AddFilter(new ExpressionFilter(args.Expression).Filter);
    }
    if (!args.TestPlan.NullOrEmpty())
    {
      TestPlan plan = DevHarmony.GetDevTool<TestPlanManager>(mod)?.FindPlan(args.TestPlan);
      if (plan != null)
      {
        testRunner.AddFilter(plan.Filter);
      }
    }
    if (!args.TestListFile.NullOrEmpty())
    {
      testRunner.AddFilter(LoadTestList(args.TestListFile));
    }
    if (testManager is { Config: ITestActions testActions })
    {
      testRunner.AddTestActions(testActions);
    }
    return testRunner;

    static TestFilter LoadTestList(string filePath)
    {
      HashSet<string> names = File.ReadAllLines(filePath).ToHashSet();
      return new TestFilter(NameFilter);

      bool NameFilter(ITestFixture fixture, ITestFunction function)
      {
        return names.Contains(fixture.FullName) || names.Contains(function.FullName);
      }
    }
  }

  public enum ManagerType
  {
    Unit,
    Smoke,
    Plan
  }

  public record Args
  {
    public string[] packageIds;
    public bool hideWindow;
    public bool exitOnFinish;
    public bool headless;

    public int? port;
    public string sessionId;

    public ManagerType ManagerType { get; set; } = ManagerType.Unit;

    public string Expression { get; set; }

    public string TestPlan { get; set; }

    public string TestListFile { get; set; }
  }
}