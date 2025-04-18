﻿using System;
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
      if (runningUnitTests == value) return;

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
    string category = attr.Category ?? type.Name;

    // Shouldn't be possible but ReSharper won't shut up so either we do a sanity check
    // or we disable the warning.
    if (category == null)
      throw new NullReferenceException(nameof(category));

    if (!unitTests.ContainsKey(category))
      unitTests[category] = new UnitTestGroup(category, attr.Type, attr.RunAsync);
    unitTests[category].AddFromType(type);
    return true;
  }

  void IDevTool.Init(ModContentPack modContentPack)
  {
    this.mod = modContentPack;

    foreach (UnitTestGroup testGroup in unitTests.Values)
    {
      testGroup.SortByExecutionPriority();
    }
    testExplorer = new Dialog_TestExplorer(this, unitTests.Values.ToList());
    ReloadTestPlans();
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
        Log.Error($"Trying to run {testGroup.Category} while disabled.");
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
    if (testGroup.RunAsync)
    {
      LongEventHandler.ExecuteWhenFinished(delegate
      {
        CoroutineObject.Instance.StartCoroutine(UnitTestRoutine(testGroup, filter));
      });
    }
  }

  private void ExecuteUnitTests(TestPlan testPlan)
  {
    LongEventHandler.ExecuteWhenFinished(delegate
    {
      CoroutineObject.Instance.StartCoroutine(TestPlanRoutine(testPlan));
    });
  }

  private IEnumerator UnitTestRoutine(UnitTestGroup testGroup, HashSet<UnitTestGroup.Method> filter)
  {
    using UnitTestEnabler ute = new(this);

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
        goto EndTest;
      if (block.type == TestType.Disabled)
        continue;

      if (currentTestType != block.type)
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

    EndTest: ;
    if (Current.ProgramState != ProgramState.Entry)
    {
      GenScene.GoToMainMenu();
      while (Current.ProgramState != ProgramState.Entry ||
        LongEventHandler.AnyEventNowOrWaiting)
      {
        if (StopRequested)
          goto EndTest;
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
        yield return new WaitForSecondsRealtime(1);
        break;
      }
      case TestType.Disabled:
      default:
        throw new NotImplementedException();
    }
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
    MemoryUtility.ClearAllMapsAndWorld();
    SetupForTest( /*block.template*/);
    PageUtility.InitGameStart();
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
      template.world.rainfall, template.world.temperature, template.world.population);
    Find.GameInitData.ChooseRandomStartingTile();
    if (template.map?.biome != null)
    {
      Find.WorldGrid[Find.GameInitData.startingTile].biome = template.map.biome;
    }

    Find.Scenario.PostIdeoChosen();
  }

  private readonly struct UnitTestEnabler : IDisposable
  {
    public UnitTestEnabler(UnitTestManager manager)
    {
      RunningUnitTests = true;
      manager.cts = new CancellationTokenSource();
    }

    void IDisposable.Dispose()
    {
      RunningUnitTests = false;
    }
  }
}