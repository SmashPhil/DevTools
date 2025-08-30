using System;
using System.Collections.Generic;
using System.Linq;
using DevTools.Benchmarking;
using DevTools.UnitTesting;
using HarmonyLib;
using LudeonTK;
using Verse;

namespace DevTools;

[StaticConstructorOnStartup]
internal static class DevHarmony
{
	private const string ModId = "DevTools";

	private static readonly Dictionary<ModContentPack, List<IDevTool>> ModDevTools = [];

	// NOTE - this should be initialized from SCOS so dev tools static constructors have access to Defs
	static DevHarmony()
	{
		Harmony.Patch(
			original: AccessTools.Method(typeof(DebugWindowsOpener), "DrawButtons"),
			postfix: new HarmonyMethod(typeof(DevHarmony),
				nameof(DrawDebugWindowButton)));

		LoadTypes();
	}

	private static Harmony Harmony { get; } = new(ModId);

	public static T GetDevTool<T>(ModContentPack mod) where T : IDevTool
	{
		if (!ModDevTools.TryGetValue(mod, out List<IDevTool> tools))
			return default;

		foreach (IDevTool tool in tools)
		{
			if (tool is T result)
				return result;
		}
		return default;
	}

	private static T CreateDevTool<T>() where T : IDevTool, new()
	{
		T tool = new();
		return tool;
	}

	private static void LoadTypes()
	{
		foreach (ModContentPack mod in LoadedModManager.RunningModsListForReading)
		{
			List<IDevTool> toolList =
			[
				CreateDevTool<BenchmarkManager>(),
				CreateDevTool<UnitTestManager>(),
			];
			bool anyRegistered = false;
			foreach (Type type in mod.assemblies.loadedAssemblies.SelectMany(assembly =>
				assembly.GetTypes()))
			{
				foreach (IDevTool devTool in toolList)
				{
					try
					{
						anyRegistered |= devTool.TryRegisterType(type);
					}
					catch (Exception ex)
					{
						Log.Error($"Exception thrown loading type {type.Name} for {devTool}.\n{ex}");
					}
				}
			}
			if (anyRegistered)
			{
				foreach (IDevTool devTool in toolList)
				{
					devTool.Init(mod);
				}
				ModDevTools[mod] = toolList;
			}
		}

		// Run commands for matching pid only
		foreach (ModContentPack mod in LoadedModManager.RunningModsListForReading)
		{
			CommandRunner.Result result = CommandRunner.ExecuteCommandLineArgs(mod);
			if (result != null)
				return;
		}
	}

	private static void OpenModMenu()
	{
		List<DebugMenuOption> options = [];
		foreach (ModContentPack mod in ModDevTools.Keys)
		{
			options.Add(
				new DebugMenuOption(mod.Name, DebugMenuOptionMode.Action, () => OpenToolMenu(mod)));
		}
		Find.WindowStack.Add(new Dialog_DebugOptionListLister(options, "Mods"));
	}

	private static void OpenToolMenu(ModContentPack mod)
	{
		List<DebugMenuOption> toolOptions = [];
		foreach (IDevTool tool in ModDevTools[mod])
		{
			toolOptions.Add(new DebugMenuOption(tool.ToolName, DebugMenuOptionMode.Action,
				tool.OpenMenu));
		}
		Find.WindowStack.Add(new Dialog_DebugOptionListLister(toolOptions, "Tools"));
	}

	private static void DrawDebugWindowButton(WidgetRow ___widgetRow, out float ___widgetRowFinalX)
	{
		if (___widgetRow.ButtonIcon(TexButton.OpenDebugActionsMenu, "DevTools"))
		{
			if (ModDevTools.Count > 1)
				OpenModMenu();
			else
				OpenToolMenu(ModDevTools.FirstOrDefault().Key);
		}
		___widgetRowFinalX = ___widgetRow.FinalX;
	}
}