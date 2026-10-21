using System;
using System.Collections;
using System.Reflection;
using DevTools.Testing;
using HarmonyLib;
using JetBrains.Annotations;
using Verse;

namespace DevTools;

[UsedImplicitly]
internal sealed class DevMod : Mod
{
  public DevMod(ModContentPack content) : base(content)
  {
    if (GenCommandLine.TryGetCommandLineArg("--test-discovery", out string packageId))
    {
      LongEventHandler.ExecuteWhenFinished(() => DiscoverTestsAndShutdown(packageId));
    }
  }

  private static void DiscoverTestsAndShutdown(string packageId)
  {
    ExecuteAtMainMenu.Start(packageId);
  }

  private class ExecuteAtMainMenu
  {
    private readonly string packageId;

    private ExecuteAtMainMenu(string packageId)
    {
      this.packageId = packageId;
    }

    public static void Start(string packageId)
    {
      ExecuteAtMainMenu exec = new(packageId);
      // Root.Shutdown calls into Steam, so this runs on the main thread. A long event runs after
      // the static constructor calls, so CoroutineObject exists by then.
      LongEventHandler.QueueLongEvent(() => CoroutineObject.Instance.StartCoroutine(exec.Execute()),
        "LoadingLongEvent", false, _ => Root.Shutdown());
    }

    // Quits even when discovery fails, so a caller waiting on the process is not left hanging.
    private IEnumerator Execute()
    {
      try
      {
        FieldInfo globalInitDone = AccessTools.Field(typeof(Root_Entry), "globalInitDone");
        while (LongEventHandler.AnyEventNowOrWaiting || !(bool)globalInitDone.GetValue(null))
        {
          yield return null;
        }
        Discover();
      }
      finally
      {
        Root.Shutdown();
      }
    }

    private void Discover()
    {
      try
      {
        ModsConfig.Save();
        ITestManager manager = DevHarmony.GetDevTool<TestFixtureManager>(packageId);
        manager.SaveMetaData();
      }
      catch (Exception ex)
      {
        Log.Error($"Test discovery for {packageId} failed.\n{ex}");
      }
    }
  }
}
