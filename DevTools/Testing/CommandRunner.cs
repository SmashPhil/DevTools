using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DevTools.Testing;
using UnityEngine;
using Verse;

namespace DevTools;

internal static class CommandRunner
{
  private const int NothingToRunExitCode = 2;

  public static Result ExecuteCommandLineArgs(ModContentPack mod)
  {
    const string PackageIdArg = "--pid";

    const string RunTestsArg = "--test";
    const string FilterArg = "--filter";
    const string WhereArg = "--where";

    const string RunPlanArg = "--test-plan";
    const string RunListArg = "--test-file";
    const string SmokeTestArg = "--smoke-test";

    const string RunTestsShort = "-t";
    const string WhereShort = "-w";
    const string RunPlanShort = "-p";
    const string SmokeTestShort = "-s";

    const string ExitAtEndArg = "--exit";
    const string ExitAtEndShort = "-e";
    const string PortArg = "--port";
    const string SessionArg = "--session";

    // Waylith
    const string NoWindow = "-NoWindow";
    const string Headless = "-Headless";

    ManagerType type = ManagerType.Unit;
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
          type = ManagerType.Unit;
          break;
        case SmokeTestArg or SmokeTestShort:
          type = ManagerType.Smoke;
          break;
        case RunPlanArg or RunPlanShort:
          type = ManagerType.Plan;
          if (i + 1 < args.Length)
          {
            result.LoadTestPlan(mod, args[++i]);
          }
          break;
        case RunListArg:
          result.TestListFile = args[++i];
          break;
        case WhereArg or WhereShort or FilterArg:
          if (i + 1 < args.Length)
            result.Expression = args[++i];
          break;
        case ExitAtEndArg or ExitAtEndShort:
          result.exitOnFinish = true;
          break;
        case NoWindow:
          result.hideWindow = true;
          result.exitOnFinish = true;
          break;
        case Headless:
          result.headless = true;
          break;
        case PortArg:
          string portStr = args[++i];
          if (!int.TryParse(portStr, out int port))
          {
            Console.Error.WriteLine($"Unable to parse port {portStr}");
          }
          else
          {
            result.port = port;
          }
          break;
        case SessionArg:
          result.sessionId = args[++i];
          break;
      }
    }
    if (result.packageId == null)
      return null;

    if (!result.packageId.EqualsIgnoreCase(mod.PackageIdPlayerFacing))
      return result;

    result.mod = mod;
    ITestManager testManager = type switch
    {
      ManagerType.Unit => DevHarmony.GetDevTool<TestFixtureManager>(mod),
      ManagerType.Smoke => DevHarmony.GetDevTool<SmokeTestManager>(mod),
      ManagerType.Plan => DevHarmony.GetDevTool<TestPlanManager>(mod),
      _ => throw new NotImplementedException(type.ToString())
    };
    if (testManager == null)
    {
      string tests = type switch
      {
        ManagerType.Smoke => "smoke tests",
        ManagerType.Plan => "test plans",
        _ => "unit tests"
      };
      NothingToRun(result, $"{mod.PackageIdPlayerFacing} has no {tests}.");
      return result;
    }
    if (type == ManagerType.Plan && result.Filter == null)
    {
      NothingToRun(result, result.TestPlan != null
        ? $"{mod.PackageIdPlayerFacing} has no test plan named {result.TestPlan}."
        : $"{RunPlanArg} needs the name of a plan.");
      return result;
    }

    TestRunner testRunner = new(testManager);
    if (result.Filter != null)
    {
      testRunner.AddFilter(result.Filter);
    }
    if (!result.Expression.NullOrEmpty())
    {
      ExpressionFilter expressionFilter;
      try
      {
        expressionFilter = new ExpressionFilter(result.Expression);
      }
      catch (Exception ex)
      {
        NothingToRun(result, $"Unable to parse filter {result.Expression}. {ex.Message}");
        return result;
      }
      testRunner.AddFilter(expressionFilter.Filter);
    }
    if (!result.TestListFile.NullOrEmpty())
    {
      TestFilter listFilter;
      try
      {
        listFilter = LoadTestList(result.TestListFile);
      }
      catch (Exception ex)
      {
        NothingToRun(result, $"Unable to read test file {result.TestListFile}. {ex.Message}");
        return result;
      }
      testRunner.AddFilter(listFilter);
    }
    if (result.port != null && !result.sessionId.NullOrEmpty())
    {
      testRunner.AddTcpConnection(result.port.Value, result.sessionId);
    }
    if (testManager is { Config: ITestActions testActions })
    {
      testRunner.AddTestActions(testActions);
    }
    testRunner.Run();
    return result;

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

  public static void NothingToRun(Result result, string reason)
  {
    Log.Error(reason);
    DevLog.Write(reason);
    if (result.exitOnFinish)
    {
      Application.Quit(NothingToRunExitCode);
    }
  }

  private enum ManagerType
  {
    Unit,
    Smoke,
    Plan
  }

  public record Result
  {
    public string packageId;
    public ModContentPack mod;
    public bool hideWindow;
    public bool exitOnFinish;
    public bool headless;

    public int? port;
    public string sessionId;

    public string Expression { get; set; }

    public string TestPlan { get; private set; }

    public string TestListFile { get; set; }

    public TestFilter Filter { get; private set; }

    public void LoadTestPlan(ModContentPack mod, string name)
    {
      TestPlan = name;
      TestPlan plan = DevHarmony.GetDevTool<TestPlanManager>(mod)?.FindPlan(name);
      // Can't use null conditional, method group conversion causes compiler error.
      Filter = plan != null ? new TestFilter(plan.Filter) : null;
    }
  }
}