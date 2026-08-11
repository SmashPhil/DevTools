using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;
using Verse;

namespace DevTools.Testing;

/// <summary>
/// Test Manager for running unit tests in RimWorld. Tests can be run in isolation or be executed
/// as part of a test suite. This manager will handle switching between scenes and consolidating
/// test results in an explorer widget, allowing you to view each test class and its results.
/// <para/>
/// Due to Unity being single threaded, tests are run synchronously. This will block the
/// main thread and cause the application to hang for the duration of test execution. Because
/// RimWorld is so tightly coupled it's impossible to predict where it might call to Unity's API.
/// </summary>
[PublicAPI]
public class TestFixtureManager : IDevToolWithMenu, ITestManager
{
  private const string ManagerName = "Test Explorer";

  // Debugging only
  internal static readonly bool BreakOnTestFailure;

  private ModContentPack mod;
  private readonly Dictionary<Assembly, ITestModule> modules = [];
  private readonly Dictionary<string, TestFixture> testFixtures = [];

  private Dialog_TestExplorer testExplorer;

  string ITestManager.ConfigName => "UnitTestConfig";

  public TestConfig Config { get; private set; }

  ITestConfig ITestManager.Config => Config;

  public IEnumerable<ITestFixture> TestFixtures => testFixtures.Values;

  string IDevToolWithMenu.Name => ManagerName;

  private ITestModule GetModule(Type type)
  {
    if (!modules.TryGetValue(type.Assembly, out ITestModule module))
    {
      module = new AssemblyModule(type.Assembly);
      modules.Add(type.Assembly, module);
    }
    return module;
  }

  public void OpenMenu()
  {
    testExplorer ??= new Dialog_TestExplorer(this);
    Find.WindowStack.Add(testExplorer);
  }

  bool IDevTool.TryRegisterType(Type type)
  {
    TestFixtureAttribute attr = type.TryGetAttribute<TestFixtureAttribute>();
    if (attr is null || type.IsAbstract)
      return false;

    if (type.MissingRequiredMods())
      return false;

    ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
    if (constructors.Length != 1)
    {
      Log.Error($"Unable to register {type.Name}. It must have 1 public constructor.");
      return false;
    }

    foreach (TestFixture testGroup in TestExtensions.CreateAllFixtures(CreateFixture, type, constructors[0]))
    {
      string key = $"{type.Namespace}.{testGroup.Name}";
      if (testGroup.Args.Length > 0)
      {
        key += $"({string.Join(',', testGroup.Args)})";
      }
      testFixtures.TryAdd(key, testGroup);
      testGroup.AddFromType(type);
    }
    return true;

    TestFixture CreateFixture()
    {
      return new TestFixture(GetModule(type), type, attr.Type);
    }
  }

  bool IDevTool.Init(ModContentPack modContentPack)
  {
    mod = modContentPack;
    foreach (TestFixture testGroup in testFixtures.Values)
    {
      testGroup.SortByExecutionPriority();
    }
    Config = this.LoadConfig<TestConfig>(mod);
    if (Config == null)
      return false;

    Test.Discover(this);
    return true;
  }

  internal bool TryGetTestFixture(string fullName, out TestFixture testGroup)
  {
    return testFixtures.TryGetValue(fullName, out testGroup);
  }

  void ITestManager.OnTestRunnerStart()
  {
  }

  void ITestManager.OnTestRunnerEnd()
  {
    if (Config.log is { report: not null })
    {
      GenGeneric.InvokeStaticGenericMethod(typeof(Test), Config.log.report, nameof(Test.GenerateReport),
        args: [Config.log.folder, this]);
    }
    if (DevHarmony.Args is { exitOnFinish: true })
    {
      bool anyFailed = TestFixtures.Any(group => group.Status == Status.Failed);
      DevLog.Write($"Test runner finished. Result: {(anyFailed ? "Failed" : "Passed")}");
      Application.Quit(anyFailed ? 1 : 0);
      return;
    }
    OpenMenu();
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
      "TestFixtureManager Aborted Operation");
    Scribe.ForceStop();
    GenScene.GoToMainMenu();
  }
}