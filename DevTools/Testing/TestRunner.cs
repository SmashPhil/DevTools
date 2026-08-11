using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;
using Verse.Profile;
using static DevTools.Testing.Expression;

namespace DevTools.Testing;

public delegate IEnumerable<(ITestFixture, List<ITestFunction>)> TestFilter(ITestManager testManager);

public delegate bool StopOnTest(ITestCase testCase);

public delegate bool TestAction(ITestFixture group);

[PublicAPI]
public sealed class TestRunner
{
  public readonly ITestManager testManager;

  private readonly ExpressionTree expressionTree;
  private readonly TestFilter testFilter;

  private readonly List<StopOnTest> stopConditions = [];

  private readonly List<TestAction> preTestActions = [];
  private readonly List<TestAction> postTestActions = [];

  /// <summary>
  /// Event for runner state changes.
  /// <para/>
  /// This event will fire when testing begins and again when it ends.
  /// </summary>
  public static event Action<bool> OnTestRunnerStateChange;

  public TestRunner(ITestManager testManager)
  {
    this.testManager = testManager;
  }

  public TestRunner(ITestManager testManager, [NotNull] TestFilter testFilter)
    : this(testManager)
  {
    this.testFilter = testFilter;
  }

  public TestRunner(ITestManager testManager, [NotNull] ExpressionTree expressionTree)
    : this(testManager)
  {
    this.expressionTree = expressionTree;
  }

  public static bool Active => Current != null;

  public static TestRunner Current { get; private set; }

  private bool StopRequested { get; set; }

  public void And(Expression expression, Comparison comparison, string value)
  {
    if (expressionTree is null)
    {
      Log.Error(
        $"Unable to add expression {expression.GetType()} to tree. TestRunner created without expressions.");
      return;
    }
    expressionTree.SetBoolean(Expression.Boolean.And);
    expressionTree.Add(expression, comparison, value);
  }

  public void Or(Expression expression, Comparison comparison, string value)
  {
    if (expressionTree is null)
    {
      Log.Error(
        $"Unable to add expression {expression.GetType()} to tree. TestRunner created without expressions.");
      return;
    }
    expressionTree.SetBoolean(Expression.Boolean.Or);
    expressionTree.Add(expression, comparison, value);
  }

  public void AddTestActions(ITestActions testActions)
  {
    AddPreTestAction(testActions.PreTest);
    AddPostTestAction(testActions.PostTest);
    AddStopCondition(testActions.ShouldStop);
  }

  public void AddStopCondition(StopOnTest stopCondition)
  {
    stopConditions.Add(stopCondition);
  }

  public void AddPreTestAction([NotNull] TestAction testAction)
  {
    preTestActions.Add(testAction);
  }

  public void AddPostTestAction([NotNull] TestAction testAction)
  {
    postTestActions.Add(testAction);
  }

  public void Run()
  {
    LongEventHandler.ExecuteWhenFinished(delegate
    {
      // Only 1 test routine can be active at a time. Tests must run on the main thread due to 
      // Unity's lack of thread safety. If multiple tests are ran at the same time, it would need
      // to be in a separate process.
      if (Active)
      {
        Log.Error("Trying to start test runner when one is already in progress.");
        return;
      }

      StopRequested = false;
      TestFilter filter = expressionTree != null ? expressionTree.GetFilteredTests : testFilter;
      // Unfiltered expression tree will return all tests in order of TestType
      filter ??= new ExpressionTree().GetFilteredTests;
      CoroutineObject.Instance.StartCoroutine(TestRoutine(filter));
    });
  }

  public static void StopIfActive()
  {
    Current?.SignalToStop();
  }

  public void SignalToStop()
  {
    StopRequested = true;
  }

  private IEnumerator TestRoutine(TestFilter filter)
  {
    ITestConfig config = testManager.Config;
    using ApplicationState appState = new();

    // Enable test logger first, TestFixtureEnabler will invoke state change events which may want
    // to write to the test log.
    using DevLog.Enabler logEnabler = new(config.LogConfig);
    using Test.SessionScope session = new(testManager);
    using TestEnabler ute = new(this);

    // Running unit tests from command line will jump into this coroutine before Root.Start has a 
    // chance to run. Skip 1 frame so all Root fields have a chance to initialize.
    yield return null;

    uint seed = config.Seed ?? (uint)Rand.Int;
    using RandBlockPersistent randBlock = new(seed);

    testManager.OnTestRunnerStart();

    DevLog.Write($"Starting tests with seed: {seed}");
    DevLog.WriteLine();
    TestType currentTestType = TestType.MainMenu;
    TestReport report = new();

    bool restoreEnvironment = false;
    // NOTE: Multi-pass enumeration is intentional for now. Setting all to pending before running
    // tests makes it easier for deciding when a test status can be overwritten by a new result,
    // such as premature skips / failures.
    // ReSharper disable PossibleMultipleEnumeration
    var testsToRun = filter(testManager);
    ResetAll(testsToRun);
    foreach ((ITestFixture fixture, List<ITestFunction> functions) in testsToRun)
    {
      if (StopRequested || ShouldStop(fixture))
        break;

      Assert.IsFalse(fixture.MissingRequiredMods());
      if (functions.NullOrEmpty())
      {
        Log.Warning($"No functions in fixture {fixture.Name}");
        continue;
      }

      using (new Test.Scope(fixture))
      {
        using LogWatcher fxWatcher = new(testManager.Config);

        object instance = fixture.CreateInstance();
        FixtureGameSettings fxtSettings = new(instance);
        // Scene change for test type
        bool hasSaveFile = !fixture.SaveFile.NullOrEmpty();
        if (currentTestType != fixture.TestType || hasSaveFile || fxtSettings.NeedsReload || restoreEnvironment)
        {
          currentTestType = fixture.TestType;
          if (hasSaveFile)
          {
            if (fxtSettings.NeedsReload)
            {
              Log.Warning($"Fixture {fixture.Name} has custom settings for game generation, " +
                          $"but is configured to load a save file.");
            }
            yield return LoadSaveRoutine(fixture.SaveFile);
          }
          else
          {
            yield return ChangeSceneRoutine(currentTestType, fixture, fxtSettings);
          }
        }
        // Restore default test environment if this fixture loaded a save or has custom game settings.
        restoreEnvironment = hasSaveFile || fxtSettings.NeedsReload;

        if (currentTestType != fixture.TestType)
        {
          Test.Fail($"Unable to transition scene to {fixture.TestType}");
          foreach (ITestFunction function in functions)
          {
            using Test.Scope fns = new(function);
            Test.Current.Status = Status.Skipped;
          }

          continue;
        }

        try
        {
          if (!RunPreTestActions(fixture))
          {
            Test.Fail($"Failed pre-test actions for {fixture.Name}!");
            continue;
          }

          if (!fixture.OneTimeSetUp(instance))
          {
            Test.Fail($"Failed to set up {fixture.Name}!");
            continue;
          }

          foreach (ITestFunction function in functions)
          {
            if (ShouldStop(function))
              break;

            Assert.IsFalse(function.MissingRequiredMods());
            int testRetries = function.MetaData.Get<ushort>(MetaDataName.RetryTest);
            int maxRetries = Mathf.Max(testRetries, config.RetryAttempts);
            int attempts = 0;
            do
            {
              using Test.Scope fns = new(function);
              using LogWatcher fnWatcher = new(testManager.Config);
              if (++attempts > 1)
              {
                DevLog.WriteVerbose("Retrying...");
              }
              try
              {
                if (!fixture.SetUp(instance))
                {
                  Test.Fail($"Failed to set up {fixture.Name}!");
                  continue;
                }

                if (function.IsSubRoutine())
                {
                  yield return function.ExecuteRoutine(instance);
                }
                else
                {
                  function.Execute(instance);
                }
              }
              finally
              {
                if (!fixture.TearDown(instance))
                {
                  Test.Fail($"Failed to tear down {fixture.Name}!");
                }
              }
            } while (attempts <= maxRetries && function.Status is Status.Failed);

            report.Add(function);

            if (ShouldStop(function))
              break;
          }
          if (!fixture.OneTimeTearDown(instance))
          {
            Test.Fail($"Failed to tear down {fixture.Name}!");
            continue;
          }
        }
        finally
        {
          try
          {
            if (!RunPostTestActions(fixture))
            {
              Test.Fail($"Failed post-test actions for {fixture.Name}!");
            }
          }
          catch (Exception ex)
          {
            Test.Fail(ex);
          }
        }
      }
      Test.LogResults(fixture, functions);
      DevLog.Flush();

      if (ShouldStop(fixture))
        break;
    }

    // Open test results at main menu
    if (Verse.Current.ProgramState != ProgramState.Entry)
    {
      GenScene.GoToMainMenu();
      while (Verse.Current.ProgramState != ProgramState.Entry ||
        LongEventHandler.AnyEventNowOrWaiting)
      {
        if (StopRequested)
          break;

        yield return null;
      }
    }

    DevLog.WriteLine();
    DevLog.Write(
      $"Tests Completed. ({report.Count(Status.Passed)} Passed, {report.Count(Status.Failed)} Failed, {report.Count(Status.Skipped)} Skipped)");
    testManager.OnTestRunnerEnd();
    yield break;

    static void ResetAll(IEnumerable<(ITestFixture, List<ITestFunction>)> tests)
    {
      foreach ((ITestFixture fixture, List<ITestFunction> functions) in tests)
      {
        Test.GetEntry(fixture).Reset();
        foreach (ITestFunction function in functions)
        {
          Test.GetEntry(function).Reset();
        }
      }
    }
  }

  private bool ShouldStop(ITestCase testCase)
  {
    foreach (StopOnTest condition in stopConditions)
    {
      if (condition(testCase))
        return true;
    }
    return false;
  }

  private bool RunPreTestActions(ITestFixture testGroup)
  {
    bool result = true;
    foreach (TestAction testAction in preTestActions)
    {
      result &= testAction(testGroup);
    }
    return result;
  }

  private bool RunPostTestActions(ITestFixture testGroup)
  {
    bool result = true;
    foreach (TestAction testAction in postTestActions)
    {
      result &= testAction(testGroup);
    }
    return result;
  }

  private static IEnumerator LoadSaveRoutine(string saveFile)
  {
    using GenStepWarningDisabler gswd = new();
    Assert.IsTrue(!saveFile.NullOrEmpty());
    GameDataSaveLoader.LoadGame(saveFile);
    yield return WaitTillPlaying();
  }

  private IEnumerator ChangeSceneRoutine(TestType testType, ITestFixture fixture, FixtureGameSettings settings)
  {
    ITestConfig config = testManager.Config;
    switch (testType)
    {
      case TestType.MainMenu:
      {
        if (Verse.Current.ProgramState != ProgramState.Entry)
        {
          yield return LoadMainMenu();
        }
        if (settings.NeedsReload)
        {
          // For post-game tests that require specific game generation settings and test at the main menu after exiting.
          yield return GenerateWorldRoutine(settings.worldGen ?? config.WorldSettings, settings.mapGen ?? config.MapSettings,
            scenario: null, storyteller: null);
          yield return LoadMainMenu();
        }
        break;
      }
      case TestType.Playing:
      {
        yield return GenerateWorldRoutine(settings.worldGen ?? config.WorldSettings, settings.mapGen ?? config.MapSettings,
          settings.scenario, settings.storyteller);
        break;
      }
      case TestType.PostGameExit:
      {
        if (settings.NeedsReload || Verse.Current.ProgramState != ProgramState.Playing)
        {
          yield return GenerateWorldRoutine(settings.worldGen ?? config.WorldSettings, settings.mapGen ?? config.MapSettings,
            scenario: null, storyteller: null);
        }
        yield return LoadMainMenu();
        break;
      }
      default:
        throw new ArgumentException("Trying to execute disabled test type.");
    }
    yield break;

    static IEnumerator LoadMainMenu()
    {
      if (Verse.Current.ProgramState != ProgramState.Entry)
      {
        GenScene.GoToMainMenu();
        while (Verse.Current.ProgramState != ProgramState.Entry || LongEventHandler.AnyEventNowOrWaiting)
        {
          yield return null;
        }
      }
    }

    static IEnumerator GenerateWorldRoutine(WorldGenerationSettings worldGenSettings,
      MapGenerationSettings mapGenSettings, [CanBeNull] Scenario scenario, [CanBeNull] Storyteller storyteller)
    {
      using GenStepWarningDisabler gswd = new();
      GenerateWorld(worldGenSettings, mapGenSettings, scenario, storyteller);
      yield return WaitTillPlaying();
    }
  }

  private static IEnumerator WaitTillPlaying()
  {
    while (Verse.Current.ProgramState == ProgramState.MapInitializing || LongEventHandler.AnyEventNowOrWaiting)
    {
      yield return null;
    }
    // Skip 1 extra frame to allow for game to execute its single tick on load
    yield return null;
  }

  private static void GenerateWorld(WorldGenerationSettings worldGenSettings,
    MapGenerationSettings mapGenSettings, Scenario scenario = null, Storyteller storyteller = null)
  {
    LongEventHandler.QueueLongEvent(delegate
    {
      Verse.Current.Game?.Dispose();
      InitGame(worldGenSettings, mapGenSettings, scenario, storyteller);
      LongEventHandler.QueueLongEvent(delegate
      {
        Find.GameInitData.PrepForMapGen();
        Find.Scenario.PreMapGenerate();
      }, "Play", "GeneratingMap", true, null);
    }, "GeneratingMap", true, GameAndMapInitExceptionHandlers.ErrorWhileGeneratingMap);
  }

  private static void InitGame(WorldGenerationSettings worldGenSettings,
    MapGenerationSettings mapGenSettings, Scenario scenario = null, Storyteller storyteller = null)
  {
    MemoryUtility.ClearAllMapsAndWorld();
    Game game = new();
    GameInitData gameInitData = new();

    if (mapGenSettings != null)
    {
      gameInitData.mapSize = mapGenSettings.size;
      gameInitData.mapGeneratorDef = mapGenSettings.mapGeneratorDef;
    }

    Verse.Current.ProgramState = ProgramState.Entry;
    Game.ClearCaches();
    Verse.Current.Game = game;
    Verse.Current.Game.InitData = gameInitData;
    Verse.Current.Game.Scenario = scenario ?? ScenarioDefOf.Crashlanded.scenario;
    Find.Scenario.PreConfigure();
    Verse.Current.Game.storyteller = storyteller ?? new Storyteller(StorytellerDefOf.Cassandra, DifficultyDefOf.Rough);
    Verse.Current.Game.World = WorldGenerator.GenerateWorld(worldGenSettings.percent,
      GenText.RandomSeedString(),
      worldGenSettings.rainfall, worldGenSettings.temperature, worldGenSettings.population,
      worldGenSettings.landmarkDensity);
    Find.GameInitData.ChooseRandomStartingTile();
    if (mapGenSettings?.biome != null)
    {
      Find.WorldGrid[Find.GameInitData.startingTile].PrimaryBiome = mapGenSettings.biome;
    }

    Find.Scenario.PostIdeoChosen();
  }

  private readonly struct TestEnabler : IDisposable
  {
    // Disables Harmony's stack trace caching for full verbosity while conducting tests
    private readonly StackTraceCacheDisabler stcDisabler;

    public TestEnabler(TestRunner runner)
    {
      stcDisabler = new StackTraceCacheDisabler();
      Current = runner;
      OnTestRunnerStateChange?.Invoke(true);
    }

    void IDisposable.Dispose()
    {
      stcDisabler.Dispose();
      Current = null;
      OnTestRunnerStateChange?.Invoke(false);
    }
  }

  private readonly struct ApplicationState : IDisposable
  {
    private readonly bool runInBackground;

    public ApplicationState()
    {
      runInBackground = Application.runInBackground;
      Application.runInBackground = true;
    }

    void IDisposable.Dispose()
    {
      Application.runInBackground = runInBackground;
    }
  }

  private readonly struct FixtureGameSettings
  {
    public readonly WorldGenerationSettings worldGen;
    public readonly Scenario scenario;
    public readonly Storyteller storyteller;
    public readonly MapGenerationSettings mapGen;
    
    public FixtureGameSettings(object instance)
    {
      worldGen = (instance as IWorldGeneration)?.WorldGenerationSettings;
      scenario = (instance as IScenario)?.Scenario;
      storyteller = (instance as IStoryteller)?.Storyteller;
      mapGen = (instance as IMapGeneration)?.MapGenerationSettings;
    }

    public bool NeedsReload => worldGen != null || scenario != null || storyteller != null || mapGen != null;
  }
}