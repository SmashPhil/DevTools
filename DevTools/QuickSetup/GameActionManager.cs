using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DevTools.UnitTesting;
using JetBrains.Annotations;
using Verse;

namespace DevTools.GameAction;

[PublicAPI]
public class GameActionManager : IDevTool
{
	private static readonly Dictionary<GameState, List<Action>> PostLoadActions = [];

	private static readonly Dictionary<string, StartupAction> actions = [];

	private static readonly List<Toggle> actionRadioButtons = [];

	private static bool NoStartupAction { get; set; }

	private static bool Enabled { get; }

	string IDevTool.ToolName => "Scenario";

	static GameActionManager()
	{
#if DEBUG
		Enabled = false;
		try
		{
			InitializeStartupActions();
		}
		catch (Exception ex)
		{
			Log.Error($"StartupAction was unable to initialize. Disabling...\nException={ex}");
			return;
		}

		Enabled = true;
		PostLoadSetup();
#endif
	}

	bool IDevTool.TryRegisterType(Type type)
	{
		throw new NotImplementedException();
	}

	void IDevTool.Init(ModContentPack mod)
	{
		throw new NotImplementedException();
	}

	void IDevTool.OpenMenu()
	{
		Find.WindowStack.Add(new Dialog_RadioButtonMenu("Startup Actions", actionRadioButtons,
			postClose: SmashMod.Serialize));
	}

	private static void InitializeStartupActions()
	{
		SmashMod.LoadFromSettings();
		PostLoadActions.Clear();
		foreach (GameState enumValue in Enum.GetValues(typeof(GameState)))
		{
			PostLoadActions.Add(enumValue, []);
		}

		actions.Clear();
		actionRadioButtons.Clear();
		actionRadioButtons.Add(new Toggle("NoStartupAction", "None", string.Empty,
			() => NoStartupAction || SmashSettings.startupAction.NullOrEmpty(), delegate(bool value)
			{
				NoStartupAction = value;
				if (NoStartupAction)
				{
					SmashSettings.startupAction = string.Empty;
				}
			}));
		NoStartupAction = true;
		List<MethodInfo> methods = [];
		foreach (Type type in GenTypes.AllTypes)
		{
			foreach (MethodInfo method in type
			 .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
			 .Where(m => !m.GetParameters().Any()))
			{
				ScenarioAttribute startupActionAttr =
					method.GetCustomAttribute<ScenarioAttribute>();
				if (startupActionAttr is not null)
				{
					string name = startupActionAttr.Name;
					if (string.IsNullOrEmpty(name))
					{
						name = method.Name;
					}

					string category = startupActionAttr.Category;
					if (string.IsNullOrEmpty(category))
					{
						category = "General";
					}

					string actionFullName = $"{category}.{name}".Replace(" ", "");
					StartupAction startupAction = new()
					{
						FullName = actionFullName,
						DisplayName = name,
						Category = category,
						GameState = startupActionAttr.GameState,
						Action = () => method.Invoke(null, [])
					};

					if (actionFullName == SmashSettings.startupAction)
					{
						NoStartupAction = false;
					}

					actions.Add(startupAction.FullName, startupAction);
					actionRadioButtons.Add(new Toggle(startupAction.FullName, startupAction.DisplayName,
						startupAction.Category,
						stateGetter: () => SmashSettings.startupAction == startupAction.FullName,
						stateSetter: delegate(bool value)
						{
							if (value)
							{
								SmashSettings.startupAction = startupAction.FullName;
							}
						}));
				}
			}
		}

		actionRadioButtons.SortBy(toggle => toggle.DisplayName);
	}

	private static void PostLoadSetup()
	{
		if (!SmashSettings.startupAction.NullOrEmpty() &&
			actions.TryGetValue(SmashSettings.startupAction, out StartupAction action))
		{
			PostLoadActions[action.GameState].Add(action.Action);
		}
	}

	internal static void ExecutePostLoadTesting()
	{
		LongEventHandler.ExecuteWhenFinished(delegate
		{
			ExecuteTesting(GameState.LoadedSave);
			ExecuteTesting(GameState.Playing);
		});
	}

	internal static void ExecuteNewGameTesting()
	{
		LongEventHandler.ExecuteWhenFinished(delegate
		{
			ExecuteTesting(GameState.NewGame);
			ExecuteTesting(GameState.Playing);
		});
	}

	internal static void ExecuteOnStartupTesting()
	{
		LongEventHandler.ExecuteWhenFinished(delegate { ExecuteTesting(GameState.OnStartup); });
	}

	private static void ExecuteTesting(GameState gameState)
	{
		if (UnitTestManager.RunningUnitTests)
			return;

		if (Enabled)
		{
			foreach (Action action in PostLoadActions[gameState])
			{
				action.Invoke();
			}
		}
	}

	private class StartupAction
	{
		public string FullName { get; set; }
		public string DisplayName { get; set; }
		public string Category { get; set; }
		public Action Action { get; set; }
		public GameState GameState { get; set; }
	}
}