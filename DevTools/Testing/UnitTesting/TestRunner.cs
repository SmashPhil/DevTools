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
	public readonly UnitTestManager unitTestManager;

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

		// NOTE - Enable test logger first, UnitTestEnabler will invoke state change events which may want
		// to write to the test log.
		using DevLog.Enabler logEnabler = new(config.log);
		using UnitTestManager.UnitTestEnabler ute = new(this);

		// Running unit tests from command line will jump into this coroutine before Root.Start has a 
		// chance to run. Skip 1 frame and then continue so all Root fields have a chance to initialize.
		yield return null;

		uint seed = config.randSeed ?? (uint)Rand.Int;
		using RandBlockPersistent randBlock = new(seed);
		DevLog.Write($"Starting tests with seed: {seed}");
		DevLog.WriteLine();
		TestType currentTestType = TestType.MainMenu;
		foreach ((ITestGroup group, List<ITestFunction> functions) in filter(unitTestManager))
		{
			if (StopRequested || ShouldStop(group))
				break;
			if (functions.NullOrEmpty() || group.IsDisabled())
				continue;

			// Scene change for test type
			if (currentTestType != group.TestType)
			{
				currentTestType = group.TestType;
				if (!group.SaveFile.NullOrEmpty())
					yield return LoadSaveRoutine(group.SaveFile);
				else
					yield return ChangeSceneRoutine(currentTestType);
			}

			try
			{
				config.RunPreTests();
				DevLog.WriteVerbose($"Setting up {group.Type.Name}");
				if (!group.SetUp())
				{
					DevLog.Write($"Failed to set up {group.Type.Name}!");
					continue;
				}
				foreach (ITestFunction function in functions)
				{
					if (function.IsDisabled())
						continue;
					if (ShouldStop(function))
						break;

					int attempts = config.retryOnFailure ? 2 : 1;
					do
					{
						using (new LogWatcher(function))
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
						}

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
				try
				{
					DevLog.WriteVerbose($"Tearing down {group.Type.Name}");
					if (!group.TearDown())
					{
						string tearDownFail = $"Failed tear down of {group.Type.Name}!";
						DevLog.Write(tearDownFail);
						group.Fail(tearDownFail);
					}

					config.RunPostTests();
				}
				catch (Exception ex)
				{
					string error = $"Exception caught during tear down. Terminating test...\n{ex}";
					DevLog.Write(error);
					group.Fail(error);
				}
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
		unitTestManager.TestRunnerFinished();
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

	private static IEnumerator LoadSaveRoutine(string saveFile)
	{
		using GenStepWarningDisabler gswd = new();
		// Handle scene transition
		Assert.IsTrue(!saveFile.NullOrEmpty());
		GameDataSaveLoader.LoadGame(saveFile);
		yield return WaitTillProgramState(ProgramState.Playing);
	}

	private IEnumerator ChangeSceneRoutine(TestType testType)
	{
		TestConfig config = unitTestManager.Config;
		switch (testType)
		{
			case TestType.MainMenu:
				if (Current.ProgramState != ProgramState.Entry)
					yield return LoadMainMenu();
			break;
			case TestType.Playing:
				Assert.IsNull(Find.World);
				yield return GenerateWorldRoutine(config.world, config.map);
			break;
			case TestType.PostGameExit:
				if (Current.ProgramState != ProgramState.Playing)
					yield return GenerateWorldRoutine(config.world, config.map);
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

		static IEnumerator GenerateWorldRoutine(WorldGenerationSettings worldGenSettings,
			MapGenerationSettings mapGenSettings)
		{
			using GenStepWarningDisabler gswd = new();
			GenerateWorld(worldGenSettings, mapGenSettings);
			yield return WaitTillProgramState(ProgramState.Playing);
		}
	}

	private static IEnumerator WaitTillProgramState(ProgramState programState)
	{
		while (Current.ProgramState != programState ||
			LongEventHandler.AnyEventNowOrWaiting)
		{
			yield return null;
		}
		// Skip 1 extra frame to allow for game to execute its single tick on load
		yield return null;
	}

	private static void GenerateWorld(WorldGenerationSettings worldGenSettings,
		MapGenerationSettings mapGenSettings)
	{
		LongEventHandler.QueueLongEvent(delegate
		{
			MemoryUtility.ClearAllMapsAndWorld();
			InitGame(worldGenSettings, mapGenSettings);
			LongEventHandler.QueueLongEvent(delegate
			{
				Find.GameInitData.PrepForMapGen();
				Find.Scenario.PreMapGenerate();
			}, "Play", "GeneratingMap", true, null);
			//Current.Game.InitNewGame();
		}, "GeneratingMap", true, GameAndMapInitExceptionHandlers.ErrorWhileGeneratingMap);
	}

	private static void InitGame(WorldGenerationSettings worldGenSettings,
		MapGenerationSettings mapGenSettings)
	{
		Game game = new();
		GameInitData gameInitData = new();

		if (mapGenSettings != null)
		{
			gameInitData.mapSize = mapGenSettings.size;
			gameInitData.mapGeneratorDef = mapGenSettings.mapGeneratorDef;
		}

		Current.ProgramState = ProgramState.Entry;
		Game.ClearCaches();
		Current.Game = game;
		Current.Game.InitData = gameInitData;
		Current.Game.Scenario = ScenarioDefOf.Crashlanded.scenario;
		Find.Scenario.PreConfigure();
		Current.Game.storyteller = new Storyteller(StorytellerDefOf.Cassandra, DifficultyDefOf.Rough);
		Current.Game.World = WorldGenerator.GenerateWorld(worldGenSettings.percent,
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
}