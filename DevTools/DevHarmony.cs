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

  private static readonly Dictionary<ModContentPack, List<IDevTool>> modDevTools = [];

  static DevHarmony()
  {
    Harmony.Patch(
      original: AccessTools.Method(typeof(DebugWindowsOpener), "DrawButtons"),
      postfix: new HarmonyMethod(typeof(DevHarmony),
        nameof(DrawDebugWindowButton)));

    LoadTypes();
  }

  internal static Harmony Harmony { get; } = new(ModId);

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
        modDevTools[mod] = toolList;
      }
    }
  }

  private static void OpenModMenu()
  {
    List<DebugMenuOption> options = [];
    foreach (ModContentPack mod in modDevTools.Keys)
    {
      options.Add(
        new DebugMenuOption(mod.Name, DebugMenuOptionMode.Action, () => OpenToolMenu(mod)));
    }
    Find.WindowStack.Add(new Dialog_DebugOptionListLister(options, "Mods"));
  }

  private static void OpenToolMenu(ModContentPack mod)
  {
    List<DebugMenuOption> toolOptions = [];
    foreach (IDevTool tool in modDevTools[mod])
    {
      toolOptions.Add(new DebugMenuOption(tool.ToolName, DebugMenuOptionMode.Action,
        tool.OpenMenu));
    }
    Find.WindowStack.Add(new Dialog_DebugOptionListLister(toolOptions, "Tools"));
  }

#region Patches

  internal static void DrawDebugWindowButton(WidgetRow ___widgetRow, ref float ___widgetRowFinalX)
  {
    if (___widgetRow.ButtonIcon(TexButton.OpenDebugActionsMenu, "DevTools"))
    {
      if (modDevTools.Count > 1)
        OpenModMenu();
      else
        OpenToolMenu(modDevTools.FirstOrDefault().Key);
    }
    ___widgetRowFinalX = ___widgetRow.FinalX;
  }

#endregion Patches
}