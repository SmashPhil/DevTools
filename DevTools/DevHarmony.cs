using System;
using System.Collections.Generic;
using System.Linq;
using DevTools.Benchmarking;
using DevTools.Testing;
using HarmonyLib;
using LudeonTK;
using Verse;

namespace DevTools;

[StaticConstructorOnStartup]
internal static class DevHarmony
{
	private const string ModId = "DevTools";

	private static readonly Dictionary<ModContentPack, List<IDevTool>> ModDevTools = [];

	public static CommandRunner.Result Args { get; private set; }

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
		using DeepProfilerScope dps = new("Loading DevTools");
		foreach (ModContentPack mod in LoadedModManager.RunningModsListForReading)
		{
			TryLoadDevTool<BenchmarkManager>(mod);
			TryLoadDevTool<TestFixtureManager>(mod);
			TryLoadDevTool<SmokeTestManager>(mod);
			TryLoadDevTool<TestPlanManager>(mod);
		}
		FinalizeInit();
		return;

		static void TryLoadDevTool<T>(ModContentPack mod) where T : IDevTool, new()
		{
			T devTool = CreateDevTool<T>();
			bool anyRegistered = false;
			foreach (Type type in mod.assemblies.loadedAssemblies.SelectMany(assembly =>
				assembly.GetTypes()))
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
			if (anyRegistered && devTool.Init(mod))
			{
				if (!ModDevTools.TryGetValue(mod, out List<IDevTool> tools))
				{
					ModDevTools[mod] = tools = [];
				}
				tools.Add(devTool);
			}
		}
	}

	private static void FinalizeInit()
	{
		Args = null;
		// Run commands for matching pid only
		foreach (ModContentPack mod in LoadedModManager.RunningModsListForReading)
		{
			Args = CommandRunner.ExecuteCommandLineArgs(mod);
			if (Args != null)
				break;
		}

		if (Args is { headless: true })
		{
			BatchModeCompatibility.Enable();
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
			if (tool is not IDevToolWithMenu devToolWithMenu)
				continue;

			toolOptions.Add(new DebugMenuOption(devToolWithMenu.Name, DebugMenuOptionMode.Action,
				devToolWithMenu.OpenMenu));
		}
		Find.WindowStack.Add(new Dialog_DebugOptionListLister(toolOptions, "Tools"));
	}

	private static void DrawDebugWindowButton(WidgetRow ___widgetRow, out float ___widgetRowFinalX)
	{
		if (___widgetRow.ButtonIcon(TexButton.OpenStatsReport, "DevTools"))
		{
      if (ModDevTools.Count > 1)
      {
        OpenModMenu();
      }
      else
      {
        OpenToolMenu(ModDevTools.FirstOrDefault().Key);
      }
		}
		___widgetRowFinalX = ___widgetRow.FinalX;
	}
}