using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

namespace DevTools.UnitTesting;

/// <summary>
/// Test Manager for running unit tests in RimWorld. Tests can be ran in isolation or be executed
/// as part of a test suite. This manager will handle switching between scenes and consolidating
/// test results in an explorer widget, allowing you to view each test class and its results.
/// <para/>
/// Due to Unity being single threaded, tests are run synchronously. This will block the
/// main thread and cause the application to hang for the duration of test execution. Because
/// RimWorld is so tightly coupled it's impossible to predict where it might call to Unity's API.
/// </summary>
[PublicAPI]
public class UnitTestManager : IDevTool
{
  private const string ManagerName = "Unit Test";

  private static TestRunner currentTestRunner;

  internal static bool breakOnTestFailure;

  private ModContentPack mod;
  private TestConfig config;
  private readonly Dictionary<string, UnitTestGroup> unitTests = [];
  private readonly Dialog_TestExplorer testExplorer;

  /// <summary>
  /// Event for UnitTest state changes.
  /// <para/>
  /// This event will fire when unit testing begins, and again when it finishes.
  /// </summary>
  [PublicAPI]
  public static event Action<bool> OnUnitTestStateChange;

  public UnitTestManager()
  {
    testExplorer = new Dialog_TestExplorer(this);
  }

  public TestConfig Config => config;

  internal IEnumerable<UnitTestGroup> UnitTests => unitTests.Values;

  internal List<TestPlan> TestPlans { get; } = [];

  public static UnitTestManager CurrentActive => currentTestRunner?.unitTestManager;

  public static bool RunningUnitTests => currentTestRunner != null;

  string IDevTool.ToolName => ManagerName;

  bool IDevTool.TryRegisterType(Type type)
  {
    UnitTestAttribute attr = type.TryGetAttribute<UnitTestAttribute>();
    if (attr is null)
      return false;
    string key = type.FullName;
    if (key == null)
      return false;

    UnitTestGroup testGroup = new(type, attr.Type);
    if (!unitTests.ContainsKey(key))
      unitTests[key] = testGroup;
    testGroup.AddFromType(type);
    testGroup.MetaData.Load(type);
    return true;
  }

  void IDevTool.Init(ModContentPack modContentPack)
  {
    mod = modContentPack;
    foreach (UnitTestGroup testGroup in unitTests.Values)
    {
      if (testGroup.TestCount == 0)
        Log.Warning($"{testGroup.Type.Name} has 0 tests. Execution will be skipped.");
    }
    foreach (UnitTestGroup testGroup in unitTests.Values)
    {
      testGroup.SortByExecutionPriority();
    }
    LoadConfig();
    ReloadTestPlans();
    ExecuteCommandLineArgs();
  }

  private void ExecuteCommandLineArgs()
  {
    const string PackageIdArg = "--pid";

    const string RunTestsArg = "--test";

    const string FilterArg = "--where";
    const string RunPlanArg = "--plan";

    bool runTests = false;
    ArgResult result = new();
    string[] args = Environment.GetCommandLineArgs();
    if (!args.NullOrEmpty())
    {
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
              string planName = args[++i];
              result.plan = TestPlans.FirstOrDefault(plan => plan.name == planName);
            }
          break;
          case FilterArg:
            if (i + 1 < args.Length)
              result.filterStr = args[++i];
          break;
        }
      }
      if (runTests && result.packageId == mod.PackageIdPlayerFacing)
      {
        if (result.plan != null)
          throw new NotSupportedException("TestPlans are not yet supported.");


        ExpressionTree expressionTree = null;
        if (!result.filterStr.NullOrEmpty())
          expressionTree = ExpressionGenerator.Create(result.filterStr);
        TestRunner testRunner =
          expressionTree != null ? GetRunnerWith(expressionTree) : new TestRunner(this);
        testRunner.Run();
      }
    }
  }

  private void ReloadTestPlans()
  {
    TestPlans.Clear();
    DirectoryInfo dirInfo = new(GenFile.ResolveCaseInsensitiveFilePath(mod.RootDir, "TestPlans"));
    if (dirInfo.Exists)
    {
      foreach (FileInfo file in dirInfo.GetFiles("*.xml", SearchOption.AllDirectories))
      {
        try
        {
          TestPlan testPlan = DirectXmlLoader.ItemFromXmlFile<TestPlan>(file.FullName);
          if (testPlan.TryDoPostLoad(this))
            TestPlans.Add(testPlan);
        }
        catch (Exception ex)
        {
          Log.Error($"Exception thrown loading TestPlan {file.FullName}.\n{ex}");
        }
      }
    }
  }

  private void LoadConfig()
  {
    const string ConfigFileName = "TestConfig.xml";

    FileInfo file = new(GenFile.ResolveCaseInsensitiveFilePath(mod.RootDir, ConfigFileName));
    if (file.Exists)
    {
      config = DirectXmlLoader.ItemFromXmlFile<TestConfig>(file.FullName);
      if (config == null)
        Log.Warning($"[{mod.PackageIdPlayerFacing}] Unable to load test config.");
    }
    // Load defaults
    config ??= new TestConfig();
  }

  internal bool TryGetUnitTest(string fullName, out UnitTestGroup testGroup)
  {
    return unitTests.TryGetValue(fullName, out testGroup);
  }

  public void OpenMenu()
  {
    Find.WindowStack.Add(testExplorer);
  }

  public void OpenLogFile()
  {
    if (!File.Exists(Config.log.FullPath))
    {
      Messages.Message("No log file to open.", MessageTypeDefOf.RejectInput);
      return;
    }
    Application.OpenURL(Config.log.FullPath);
  }

  public void ClearTestResults()
  {
    // Parents should propagate their reset to containing functions / groups
    foreach (ITestGroup testGroup in unitTests.Values)
      testGroup.Reset();
  }

  public TestRunner GetRunnerWith([NotNull] TestFilter filter)
  {
    return new TestRunner(this, filter);
  }

  public TestRunner GetRunnerWith([NotNull] ExpressionTree expressionTree)
  {
    return new TestRunner(this, expressionTree);
  }

  public TestRunner GetRunnerWith<T>(Expression.Comparison comparison, string value)
    where T : Expression, new()
  {
    return GetRunnerWith(new T(), comparison, value);
  }

  public TestRunner GetRunnerWith(Expression expression, Expression.Comparison comparison,
    string value)
  {
    ExpressionTree tree = new();
    tree.Add(expression, comparison, value);
    return new TestRunner(this, tree);
  }

  public void RunAll()
  {
    new TestRunner(this).Run();
  }

  public void StopTesting()
  {
    if (!RunningUnitTests)
      return;
    currentTestRunner.SignalToStop();
  }

  private static void TestExceptionHandler(Exception ex)
  {
    DelayedErrorWindowRequest.Add($"Exception thrown while running tests.\n{ex}",
      "UnitTestManager Aborted Operation");
    Scribe.ForceStop();
    GenScene.GoToMainMenu();
  }

  private record ArgResult
  {
    public string packageId;
    public TestPlan plan;
    public string filterStr;
  }

  internal class UnitTestEnabler : IDisposable
  {
    // Disables Harmony's stack trace caching for full verbosity while conducting unit tests
    private readonly StackTraceCacheDisabler stcDisabler;

    public UnitTestEnabler(TestRunner runner)
    {
      stcDisabler = new StackTraceCacheDisabler();
      currentTestRunner = runner;
      OnUnitTestStateChange?.Invoke(true);
    }

    void IDisposable.Dispose()
    {
      stcDisabler.Dispose();
      currentTestRunner = null;
      OnUnitTestStateChange?.Invoke(false);
    }
  }
}