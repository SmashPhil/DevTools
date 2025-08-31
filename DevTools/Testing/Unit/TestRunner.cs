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

public delegate IEnumerable<(ITestGroup, List<ITestFunction>)> TestFilter(ITestManager testManager);

public delegate bool StopOnTest(ITestCase testCase);

public delegate bool TestAction(ITestGroup group);

[PublicAPI]
public sealed class TestRunner
{
	private static TestRunner current;

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

	public static bool Active => current != null;

	public static TestRunner Current => current;

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
		// Only 1 test routine can be active at a time. Tests must run on the main thread due to 
		// Unity's lack of thread safety. If multiple tests are ran at the same time, it would need
		// to be in a separate process.
		if (Active)
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
			CoroutineObject.Instance.StartCoroutine(TestRoutine(filter));
		});
	}

	public static void StopIfActive()
	{
		if (!Active)
			return;
		current.SignalToStop();
	}

	public void SignalToStop()
	{
		StopRequested = true;
	}

	private IEnumerator TestRoutine(TestFilter filter)
	{
		ITestConfig config = testManager.Config;
		Application.runInBackground = true;

		// NOTE - Enable test logger first, UnitTestEnabler will invoke state change events which may want
		// to write to the test log.
		using DevLog.Enabler logEnabler = new(config.LogConfig);
		using TestEnabler ute = new(this);

		// Running unit tests from command line will jump into this coroutine before Root.Start has a 
		// chance to run. Skip 1 frame and then continue so all Root fields have a chance to initialize.
		yield return null;

		uint seed = config.Seed ?? (uint)Rand.Int;
		using RandBlockPersistent randBlock = new(seed);
		DevLog.Write($"Starting tests with seed: {seed}");
		DevLog.WriteLine();
		TestType currentTestType = TestType.MainMenu;
		foreach ((ITestGroup group, List<ITestFunction> functions) in filter(testManager))
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
				if (!RunPreTestActions(group))
				{
					DevLog.Write($"Failed pre-test actions for {group.Name}!");
					continue;
				}
				DevLog.WriteVerbose($"Setting up {group.Name}");
				if (!group.SetUp())
				{
					DevLog.Write($"Failed to set up {group.Name}!");
					continue;
				}
				foreach (ITestFunction function in functions)
				{
					if (function.IsDisabled())
						continue;
					if (ShouldStop(function))
						break;

					int attempts = config.RetryAttempts + 1;
					do
					{
						using (new LogWatcher(config, function))
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
					DevLog.WriteVerbose($"Tearing down {group.Name}");
					if (!group.TearDown())
					{
						string tearDownFail = $"Failed tear down of {group.Name}!";
						DevLog.Write(tearDownFail);
						group.Fail(tearDownFail);
					}
					if (!RunPostTestActions(group))
					{
						string postTestFail = $"Failed post-test actions for {group.Name}!";
						DevLog.Write(postTestFail);
						group.Fail(postTestFail);
					}
				}
				catch (Exception ex)
				{
					string error = $"Exception caught during tear down.\n{ex}";
					DevLog.Write(error);
					group.Fail(error);
				}
			}
			if (ShouldStop(group))
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
		testManager.OnTestRunnerEnd();
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

	private bool RunPreTestActions(ITestGroup testGroup)
	{
		bool result = true;
		foreach (TestAction testAction in preTestActions)
		{
			result &= testAction(testGroup);
		}
		return result;
	}

	private bool RunPostTestActions(ITestGroup testGroup)
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
		// Handle scene transition
		Assert.IsTrue(!saveFile.NullOrEmpty());
		GameDataSaveLoader.LoadGame(saveFile);
		yield return WaitTillProgramState(ProgramState.Playing);
	}

	private IEnumerator ChangeSceneRoutine(TestType testType)
	{
		ITestConfig config = testManager.Config;
		switch (testType)
		{
			case TestType.MainMenu:
				if (Verse.Current.ProgramState != ProgramState.Entry)
					yield return LoadMainMenu();
			break;
			case TestType.Playing:
				Assert.IsNull(Find.World);
				yield return GenerateWorldRoutine(config.WorldSettings, config.MapSettings);
			break;
			case TestType.PostGameExit:
				if (Verse.Current.ProgramState != ProgramState.Playing)
					yield return GenerateWorldRoutine(config.WorldSettings, config.MapSettings);
				Assert.IsTrue(Verse.Current.ProgramState != ProgramState.Entry);
				yield return LoadMainMenu();
			break;
			default:
				throw new ArgumentException("Trying to execute disabled test type.");
		}
		yield break;

		static IEnumerator LoadMainMenu()
		{
			if (Verse.Current.ProgramState != ProgramState.Entry)
			{
				GenScene.GoToMainMenu();
				while (Verse.Current.ProgramState != ProgramState.Entry ||
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
		while (Verse.Current.ProgramState != programState ||
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

		Verse.Current.ProgramState = ProgramState.Entry;
		Game.ClearCaches();
		Verse.Current.Game = game;
		Verse.Current.Game.InitData = gameInitData;
		Verse.Current.Game.Scenario = ScenarioDefOf.Crashlanded.scenario;
		Find.Scenario.PreConfigure();
		Verse.Current.Game.storyteller = new Storyteller(StorytellerDefOf.Cassandra, DifficultyDefOf.Rough);
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
		// Disables Harmony's stack trace caching for full verbosity while conducting unit tests
		private readonly StackTraceCacheDisabler stcDisabler;

		public TestEnabler(TestRunner runner)
		{
			stcDisabler = new StackTraceCacheDisabler();
			current = runner;
			OnTestRunnerStateChange?.Invoke(true);
		}

		void IDisposable.Dispose()
		{
			stcDisabler.Dispose();
			current = null;
			OnTestRunnerStateChange?.Invoke(false);
		}
	}
}