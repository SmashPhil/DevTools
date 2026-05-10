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

  string IDevToolWithMenu.Name => "Test Plan";

  string ITestManager.ConfigName => "TestPlanConfig";

  ITestConfig ITestManager.Config { get; } = new TestConfig();

  public IEnumerable<ITestFixture> TestFixtures
  {
    get
    {
      foreach (TestPlan plan in testPlans)
      {
        foreach (ITestFixture fixture in plan.Fixtures)
        {
          yield return fixture;
        }
      }
    }
  }

  bool IDevTool.Init(ModContentPack mod)
  {
    testPlans = LoadTestPlans(mod);
    if (testPlans.NullOrEmpty())
      return false;

    Test.Discover(this);
    return true;
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
          PlanModule module = new(mod, testPlan);
          foreach (TestPlanJob job in testPlan.jobs)
          {
            TestPlanFixture fixture = new(module, job);
            fixture.MetaData.Load(typeof(TestProcess));
            fixture.Args = [];
            testPlan.Fixtures.Add(fixture);
            fixture.AddFromType(typeof(TestProcess));
          }
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
      bool anyFailed = TestFixtures.Any(group => group.Status == Status.Failed);
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
    new TestRunner(this, TestFilter).Run();
    LongEventHandler.ExecuteWhenFinished(delegate { CoroutineObject.Instance.StartCoroutine(OpenMenuRoutine()); });
    return;

    IEnumerable<(ITestFixture, List<ITestFunction>)> TestFilter(ITestManager testManager)
    {
      foreach (ITestFixture fixture in testPlan.Fixtures)
      {
        yield return (fixture, fixture.TestFunctions.ToList());
      }
    }
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

  private IEnumerator OpenMenuRoutine()
  {
    while (Current.ProgramState is not ProgramState.Entry || Find.WindowStack is null)
      yield return null;

    OpenMenu();
  }

  public void OpenMenu()
  {
    testExplorer ??= new Dialog_TestExplorer(this);
    Find.WindowStack.Add(testExplorer);
  }
}