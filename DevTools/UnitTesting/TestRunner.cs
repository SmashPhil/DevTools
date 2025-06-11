using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using UnityEngine.Assertions;
using Verse;
using Verse.Profile;
using static DevTools.UnitTesting.Expression;

namespace DevTools.UnitTesting;

public delegate IEnumerable<(ITestGroup, List<ITestFunction>)> TestFilter(
  UnitTestManager testManager);

public delegate bool StopOnTestGroup(ITestGroup function);

public delegate bool StopOnTestFunction(ITestFunction function);

[PublicAPI]
public sealed class TestRunner
{
  private readonly UnitTestManager unitTestManager;

  private readonly TestPlan testPlan;
  private readonly ExpressionTree expressionTree;
  private readonly TestFilter testFilter;

  private readonly List<StopOnTestGroup> stopOnTestGroups = [];
  private readonly List<StopOnTestFunction> stopOnTestFunctions = [];

  public TestRunner(UnitTestManager unitTestManager)
  {
    this.unitTestManager = unitTestManager;
  }

  public TestRunner(UnitTestManager unitTestManager, [NotNull] TestFilter testFilter)
    : this(unitTestManager)
  {
    this.testFilter = testFilter;
  }

  public TestRunner(UnitTestManager unitTestManager, [NotNull] TestPlan testPlan)
    : this(unitTestManager)
  {
    this.testPlan = testPlan;
  }

  public TestRunner(UnitTestManager unitTestManager, [NotNull] ExpressionTree expressionTree)
    : this(unitTestManager)
  {
    this.expressionTree = expressionTree;
  }

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

  public void AddStopCondition(StopOnTestGroup stopCondition)
  {
    stopOnTestGroups.Add(stopCondition);
  }

  public void AddStopCondition(StopOnTestFunction stopCondition)
  {
    stopOnTestFunctions.Add(stopCondition);
  }

  public void Run()
  {
    // Only 1 test routine can be active at a time. Unit tests must run on the main thread due to 
    // Unity's lack of thread safety. If multiple tests are to run at the same time, it would need
    // to be in a separate process.
    if (UnitTestManager.RunningUnitTests)
    {
      Messages.Message("Unit testing already in progress.", MessageTypeDefOf.RejectInput,
        historical: false);
      return;
    }
    LongEventHandler.ExecuteWhenFinished(delegate
    {
      StopRequested = false;
      TestFilter filter = expressionTree != null ? expressionTree.GetFilteredTests : testFilter;
      // Unfiltered expression tree will return all tests in order of TestType
      filter ??= new ExpressionTree().GetFilteredTests;

      if (unitTestManager.Config.stopOnFailure)
      {
        AddStopCondition((ITestGroup group) => group.Status == Status.Failed);
        AddStopCondition((ITestFunction function) => function.Status == Status.Failed);
      }
      CoroutineObject.Instance.StartCoroutine(TestRoutine(filter));
    });
  }

  public void SignalToStop()
  {
    StopRequested = true;
  }

  private IEnumerator TestRoutine(TestFilter filter)
  {
    TestConfig config = unitTestManager.Config;

    using UnitTestManager.UnitTestEnabler ute = new(this);
    // Enables test logger, separate from unity log
    using DevLog.Enabler logEnabler = new(config.log);
    // Observes unity log for warning and error count during unit tests
    using LogWatcher logWatcher = new(unitTestManager);

    config.RunPreTests();

    int seed = config.randSeed ?? Rand.Int;
    using RandBlock randBlock = new(seed);
    DevLog.Write($"Starting tests with seed: {seed}");
    DevLog.WriteLine();
    TestType currentTestType = TestType.MainMenu;
    foreach ((ITestGroup group, List<ITestFunction> functions) in filter(unitTestManager))
    {
      if (StopRequested)
        break;
      if (group.IsDisabled())
        continue;
      if (ShouldStop(group))
        break;

      // Scene change for test type
      if (currentTestType != group.TestType)
      {
        currentTestType = group.TestType;
        yield return SceneChangeRoutine(currentTestType, group.SaveFile);
      }

      try
      {
        DevLog.WriteVerbose($"Setting up {group.Type.Name}");
        if (!group.SetUp())
        {
          DevLog.Write($"Failed to set up {group.Type.Name}!");
          continue;
        }
        group.VerifyAllLogsAndFlush(logWatcher);
        foreach (ITestFunction function in functions)
        {
          if (function.IsDisabled())
            continue;
          if (ShouldStop(function))
            break;

          int attempts = config.retryOnFailure ? 2 : 1;
          do
          {
            // Execute tests
            if (function.IsSubRoutine())
            {
              yield return function.ExecuteRoutine();
            }
            else
            {
              function.Execute();
            }
            function.VerifyAllLogsAndFlush(logWatcher);

            if (function.Status != Status.Failed)
              break;

            if (--attempts > 0)
              DevLog.WriteVerbose("Retrying...");
          } while (attempts > 0);

          if (ShouldStop(function))
            break;
        }
      }
      finally
      {
        DevLog.WriteVerbose($"Tearing down {group.Type.Name}");
        if (!group.TearDown())
          DevLog.Write($"Failed tear down of {group.Type.Name}!");
        group.VerifyAllLogsAndFlush(logWatcher);
      }
      if (ShouldStop(group))
        break;
    }

    // Open test results at main menu
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
    config.RunPostTests();
    unitTestManager.OpenMenu();
  }

  private bool ShouldStop(ITestGroup group)
  {
    foreach (StopOnTestGroup condition in stopOnTestGroups)
    {
      if (condition(group))
        return true;
    }
    return false;
  }

  private bool ShouldStop(ITestFunction function)
  {
    foreach (StopOnTestFunction condition in stopOnTestFunctions)
    {
      if (condition(function))
        return true;
    }
    return false;
  }

  private static IEnumerator SceneChangeRoutine(TestType testType, string saveFile = null)
  {
    switch (testType)
    {
      case TestType.MainMenu:
        if (Current.ProgramState != ProgramState.Entry)
          yield return LoadMainMenu();
      break;
      case TestType.Playing:
        yield return LoadGame(saveFile);
      break;
      case TestType.PostGameExit:
        if (Current.ProgramState != ProgramState.Playing)
          yield return LoadGame(null);
        Assert.IsTrue(Current.ProgramState != ProgramState.Entry);
        yield return LoadMainMenu();
      break;
      default:
        throw new ArgumentException("Trying to execute disabled test type.");
    }
    yield break;

    static IEnumerator LoadMainMenu()
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
    }

    static IEnumerator LoadGame(string saveFile)
    {
      // Handle scene transition
      if (!saveFile.NullOrEmpty())
        GameDataSaveLoader.LoadGame(saveFile);
      else
        GenerateMap();

      while (Current.ProgramState != ProgramState.Playing ||
        LongEventHandler.AnyEventNowOrWaiting)
      {
        yield return null;
      }
      // Skip 1 extra frame to allow for game to execute its single tick on load
      yield return null;
    }
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
}