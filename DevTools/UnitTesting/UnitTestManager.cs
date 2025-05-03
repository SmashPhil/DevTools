using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Profile;

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
public class UnitTestManager : IDevTool
{
  private const string ManagerName = "Unit Test";

  private static bool runningUnitTests;

  private ModContentPack mod;
  private readonly Dictionary<string, UnitTestGroup> unitTests = [];
  private Dialog_TestExplorer testExplorer;

  private CancellationTokenSource cts;

  internal static bool breakOnTestFailure;

  /// <summary>
  /// Event for UnitTest state changes.
  /// <para/>
  /// This event will fire when unit testing begins, and again when it finishes.
  /// </summary>
  [UsedImplicitly]
  public static event Action<bool> OnUnitTestStateChange;

  internal List<TestPlan> TestPlans { get; } = [];

  public static bool RunningUnitTests
  {
    get { return runningUnitTests; }
    private set
    {
      if (runningUnitTests == value)
        return;

      runningUnitTests = value;
      OnUnitTestStateChange?.Invoke(runningUnitTests);
    }
  }

  private bool StopRequested => cts is { IsCancellationRequested: true };

  string IDevTool.ToolName => ManagerName;

  bool IDevTool.TryRegisterType(Type type)
  {
    UnitTestAttribute attr = type.TryGetAttribute<UnitTestAttribute>();
    if (attr is null)
      return false;
    string category = attr.Alias ?? type.Name;
    UnitTestGroup testGroup = new(category, attr.Type);
    if (!unitTests.ContainsKey(category))
      unitTests[category] = testGroup;
    testGroup.AddFromType(type);
    testGroup.MetaData.Load(type);
    return true;
  }

  void IDevTool.Init(ModContentPack modContentPack)
  {
    this.mod = modContentPack;
    foreach (UnitTestGroup testGroup in unitTests.Values)
    {
      if (testGroup.TestCount == 0)
        Log.Warning($"{testGroup.Alias} has 0 tests. Execution will be skipped.");
    }
    foreach (UnitTestGroup testGroup in unitTests.Values)
    {
      testGroup.SortByExecutionPriority();
    }
    testExplorer = new Dialog_TestExplorer(this, unitTests.Values.ToList());
    ReloadTestPlans();
    ExecuteCommandLineArgs();
  }

  private void ExecuteCommandLineArgs()
  {
    const string RunMod = "-pid";
    const string RunPlan = "--test-plan";
    const string RunAll = "--test-all";

    ArgResult result = new();
    string[] args = Environment.GetCommandLineArgs();
    if (!args.NullOrEmpty())
    {
      for (int i = 0; i < args.Length; i++)
      {
        string arg = args[i];
        switch (arg)
        {
          case RunMod:
            if (i + 1 < args.Length)
              result.packageId = args[++i];
            break;
          case RunPlan:
            if (i + 1 < args.Length)
            {
              string planName = args[++i];
              result.plan = TestPlans.FirstOrDefault(plan => plan.name == planName);
            }
            break;
          case RunAll:
            result.runAll = true;
            break;
        }
      }
      if (result.packageId == mod.PackageIdPlayerFacing)
      {
        if (result.plan != null)
          ExecuteUnitTests(result.plan);
        else if (result.runAll)
          ExecuteAllUnitTests();
        else
          throw new ArgumentException();
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

  internal bool TryGetUnitTest(string category, out UnitTestGroup testGroup)
  {
    return unitTests.TryGetValue(category, out testGroup);
  }

  public void OpenMenu()
  {
    Find.WindowStack.Add(testExplorer);
  }

  internal void Run(List<UnitTestGroup> testGroups, HashSet<UnitTestGroup.Method> filter = null)
  {
    if (RunningUnitTests)
    {
      Messages.Message("Unit testing already in progress.", MessageTypeDefOf.RejectInput,
        historical: false);
      return;
    }
    foreach (UnitTestGroup testGroup in testGroups)
    {
      if (testGroup.Type == TestType.Disabled)
      {
        Log.Error($"Trying to run {testGroup.Alias} while disabled.");
        return;
      }
      ExecuteUnitTests(testGroup, filter);
    }
  }

  internal void RunPlan(TestPlan testPlan)
  {
    if (RunningUnitTests)
    {
      Messages.Message("Unit testing already in progress.", MessageTypeDefOf.RejectInput,
        historical: false);
      return;
    }
    ExecuteUnitTests(testPlan);
  }

  private void ExecuteUnitTests(UnitTestGroup testGroup, HashSet<UnitTestGroup.Method> filter)
  {
    LongEventHandler.ExecuteWhenFinished(delegate
    {
      CoroutineObject.Instance.StartCoroutine(UnitTestRoutine(testGroup, filter));
    });
  }

  private void ExecuteUnitTests(TestPlan testPlan)
  {
    LongEventHandler.ExecuteWhenFinished(delegate
    {
      CoroutineObject.Instance.StartCoroutine(TestPlanRoutine(testPlan));
    });
  }

  private void ExecuteAllUnitTests()
  {
    LongEventHandler.ExecuteWhenFinished(delegate
    {
      CoroutineObject.Instance.StartCoroutine(TestAllRoutine());
    });
  }

  private IEnumerator UnitTestRoutine(UnitTestGroup testGroup, HashSet<UnitTestGroup.Method> filter)
  {
    using UnitTestEnabler ute = new(this);

    string saveFileName = testGroup.MetaData.Get<string>(MetaDataName.LoadSave);
    if (!saveFileName.NullOrEmpty())
    {
      GameDataSaveLoader.LoadGame(saveFileName);
      while (Current.ProgramState != ProgramState.Playing ||
        LongEventHandler.AnyEventNowOrWaiting)
      {
        yield return null;
      }
      yield return new WaitForSecondsRealtime(0.25f);
    }

    foreach (object obj in SceneChangeRoutine(testGroup.Type))
      yield return obj;

    testGroup.Execute(cts.Token, filter);

    if (Current.ProgramState != ProgramState.Entry)
    {
      GenScene.GoToMainMenu();
      while (Current.ProgramState != ProgramState.Entry ||
        LongEventHandler.AnyEventNowOrWaiting)
      {
        yield return null;
      }
    }
    OpenMenu();
  }

  private IEnumerator TestPlanRoutine(TestPlan testPlan)
  {
    using UnitTestEnabler ute = new(this);

    TestType currentTestType = TestType.Disabled;
    foreach (TestBlock block in testPlan.plan)
    {
      if (StopRequested)
        break;
      if (block.type == TestType.Disabled)
        continue;

      if (!block.saveFile.NullOrEmpty())
      {
        GameDataSaveLoader.LoadGame(block.saveFile);
        while (Current.ProgramState != ProgramState.Playing ||
          LongEventHandler.AnyEventNowOrWaiting)
        {
          yield return null;
        }
        yield return new WaitForSecondsRealtime(0.25f);
      }
      else if (currentTestType != block.type)
      {
        // Transition between scenes
        currentTestType = block.type;

        foreach (object obj in SceneChangeRoutine(currentTestType))
          yield return obj;
      }

      foreach (UnitTestGroup testGroup in block.UnitTests)
      {
        testGroup.Execute(cts.Token);
      }
    }

    if (Current.ProgramState != ProgramState.Entry)
    {
      GenScene.GoToMainMenu();
      while (Current.ProgramState != ProgramState.Entry ||
        LongEventHandler.AnyEventNowOrWaiting)
      {
        if (StopRequested)
          break;
        yield return null;
      }
    }
    OpenMenu();
  }

  private IEnumerator TestAllRoutine()
  {
    using UnitTestEnabler ute = new(this);

    List<UnitTestGroup> groups = unitTests.Values.OrderBy(group => group.Type == TestType.MainMenu)
     .ThenBy(group => group.Type == TestType.Playing)
     .ThenBy(group => group.Type == TestType.PostGameExit).ToList();
    TestType currentTestType = TestType.Disabled;
    foreach (UnitTestGroup testGroup in groups)
    {
      if (StopRequested)
        break;
      if (testGroup.Type == TestType.Disabled)
        continue;

      string saveFile = testGroup.MetaData.Get<string>(MetaDataName.LoadSave);
      if (!saveFile.NullOrEmpty())
      {
        GameDataSaveLoader.LoadGame(saveFile);
        while (Current.ProgramState != ProgramState.Playing ||
          LongEventHandler.AnyEventNowOrWaiting)
        {
          yield return null;
        }
        yield return new WaitForSecondsRealtime(0.25f);
      }
      else if (currentTestType != testGroup.Type)
      {
        // Transition between scenes
        currentTestType = testGroup.Type;
        foreach (object obj in SceneChangeRoutine(currentTestType))
          yield return obj;
      }
      testGroup.Execute(cts.Token);
    }

    if (Current.ProgramState != ProgramState.Entry)
    {
      GenScene.GoToMainMenu();
      while (Current.ProgramState != ProgramState.Entry ||
        LongEventHandler.AnyEventNowOrWaiting)
      {
        if (StopRequested)
          break;
        yield return null;
      }
    }
    OpenMenu();
  }

  private static IEnumerable SceneChangeRoutine(TestType testType)
  {
    switch (testType)
    {
      case TestType.MainMenu:
      {
        if (Current.ProgramState != ProgramState.Entry)
        {
          GenScene.GoToMainMenu();
          while (Current.ProgramState != ProgramState.Entry ||
            LongEventHandler.AnyEventNowOrWaiting)
          {
            yield return null;
          }
        }
        break;
      }
      case TestType.Playing:
      {
        GenerateMap();
        while (Current.ProgramState != ProgramState.Playing ||
          LongEventHandler.AnyEventNowOrWaiting)
        {
          yield return null;
        }
        break;
      }
      case TestType.PostGameExit:
      {
        if (Current.ProgramState != ProgramState.Playing)
        {
          GenerateMap();
          while (Current.ProgramState != ProgramState.Playing ||
            LongEventHandler.AnyEventNowOrWaiting)
          {
            yield return null;
          }
        }
        GenScene.GoToMainMenu();
        while (Current.ProgramState != ProgramState.Entry ||
          LongEventHandler.AnyEventNowOrWaiting)
        {
          yield return null;
        }
        break;
      }
      case TestType.Disabled:
      default:
        throw new ArgumentException("Trying to execute disabled test type.");
    }
    yield return new WaitForSecondsRealtime(0.25f);
  }

  private static void TestExceptionHandler(Exception ex)
  {
    DelayedErrorWindowRequest.Add($"Exception thrown while running tests.\n{ex}",
      "UnitTestManager Aborted Operation");
    Scribe.ForceStop();
    GenScene.GoToMainMenu();
  }

  private static void GenerateMap( /*TestBlock block*/)
  {
    LongEventHandler.QueueLongEvent(delegate
    {
      MemoryUtility.ClearAllMapsAndWorld();
      SetupForTest( /*block.template*/);
      PageUtility.InitGameStart();
    }, "GeneratingMap", true, GameAndMapInitExceptionHandlers.ErrorWhileGeneratingMap);
  }

  private static void SetupForTest(GenerationTemplate template = null)
  {
    // If template is null, default to QuickTest parameters
    if (template == null)
    {
      Root_Play.SetupForQuickTestPlay();
      return;
    }

    Current.ProgramState = ProgramState.Entry;
    Current.Game = new Game();
    Current.Game.InitData = new GameInitData();
    Current.Game.Scenario = ScenarioDefOf.Crashlanded.scenario;
    Find.Scenario.PreConfigure();
    Current.Game.storyteller = new Storyteller(StorytellerDefOf.Cassandra, DifficultyDefOf.Rough);

    Current.Game.World = WorldGenerator.GenerateWorld(template.world.percent,
      GenText.RandomSeedString(),
      template.world.rainfall, template.world.temperature, template.world.population,
      template.world.landmarkDensity);
    Find.GameInitData.ChooseRandomStartingTile();
    if (template.map?.biome != null)
    {
      Find.WorldGrid[Find.GameInitData.startingTile].PrimaryBiome = template.map.biome;
    }

    Find.Scenario.PostIdeoChosen();
  }

  private record ArgResult
  {
    public string packageId;
    public TestPlan plan;
    public bool runAll;
  }

  private readonly struct UnitTestEnabler : IDisposable
  {
    private readonly StackTraceCacheDisabler stcDisabler;
    private readonly Test.TestLogger logger;

    public UnitTestEnabler(UnitTestManager manager)
    {
      logger = new Test.TestLogger();
      stcDisabler = new StackTraceCacheDisabler();
      RunningUnitTests = true;
      manager.cts = new CancellationTokenSource();
    }

    void IDisposable.Dispose()
    {
      stcDisabler.Dispose();
      logger.Dispose();
      RunningUnitTests = false;
    }
  }
}