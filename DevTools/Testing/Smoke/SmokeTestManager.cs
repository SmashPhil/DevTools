using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;

namespace DevTools.Testing;

/// <summary>
/// Simple smoke test for detecting startup errors and doing a quick runthrough of all defs added by this modlist
/// to verify that there are no immediate startup or runtime errors.
/// </summary>
internal class SmokeTestManager : IDevToolWithMenu, ITestManager
{
  private readonly Dictionary<Assembly, ITestModule> modules = [];
  private readonly Dictionary<(string, TestType), SmokeTestFixture> smokeTests = [];

  private TestConfig config;

  string IDevToolWithMenu.Name => "Smoke Test";

  string ITestManager.ConfigName => "SmokeTestConfig";

  public ITestConfig Config => config;

  public IEnumerable<ITestFixture> TestFixtures => smokeTests.Values;

  private ITestModule GetModule(Type type)
  {
    if (!modules.TryGetValue(type.Assembly, out ITestModule module))
    {
      module = new AssemblyModule(type.Assembly);
      modules.Add(type.Assembly, module);
    }
    return module;
  }

  bool IDevTool.Init(ModContentPack mod)
  {
    config = this.LoadConfig<TestConfig>(mod);
    if (config == null)
      return false;

    Test.Discover(this);
    return true;
  }

  bool IDevTool.TryRegisterType(Type type)
  {
    bool anyAdded = false;

    foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static |
      BindingFlags.Instance))
    {
      if (method.TryGetAttribute<SmokeTestAttribute>() is not { } smokeTestAttr)
        continue;

      string key = type.FullName;
      if (key == null)
        return false;

      bool added = !smokeTests.TryGetValue((key, smokeTestAttr.Type), out SmokeTestFixture testGroup);
      testGroup ??= new SmokeTestFixture(GetModule(type), type, smokeTestAttr.Type);
      testGroup.MetaData.Load(type);

      if (testGroup.MetaData.Get<bool>(MetaDataName.Disabled))
        continue;

      if (added)
      {
        smokeTests[(key, smokeTestAttr.Type)] = testGroup;
      }
      anyAdded |= testGroup.TryAddFunction(method);
    }
    return anyAdded;
  }

  void ITestManager.OnTestRunnerStart()
  {
  }

  void ITestManager.OnTestRunnerEnd()
  {
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
    new TestRunner(this).Run();
  }

  // If smoke test is launched without batch mode, the results need to be shown somewhere. This menu is otherwise
  // inaccessible in-game as there is no reason to launch a 'smoke test' from main menu yet.
  // TODO - Add def spawning as an option for smoke tests (i.e. the default for menu-launched smoke tests)
  public void OpenMenu()
  {
    Find.WindowStack.Add(new Dialog_TestExplorer(this));
  }
}