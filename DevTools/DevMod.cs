using System.Threading;
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
      Thread thread = new(exec.Execute) { IsBackground = true };
      thread.Start();
    }

    private void Execute()
    {
      do
      {
        Thread.Sleep(200);
      } while (LongEventHandler.AnyEventNowOrWaiting ||
               !typeof(Root_Entry).StaticFieldRefAccess<bool>("globalInitDone"));

      ModsConfig.Save();
      ITestManager manager = DevHarmony.GetDevTool<TestFixtureManager>(packageId);
      manager.SaveMetaData();
      Root.Shutdown();
    }
  }
}
