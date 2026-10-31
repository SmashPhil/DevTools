using System;
using System.Collections;
using DevTools.Testing;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using Verse;
using Waylith.Audio;

namespace DevTools;

[UsedImplicitly]
internal sealed class DevMod : Mod
{
  public DevMod(ModContentPack content) : base(content)
  {
    if (GenCommandLine.TryGetCommandLineArg("--test-discovery", out string packageId))
    {
      LongEventHandler.ExecuteWhenFinished(() => GenerateTestManifest(packageId));
    }
    if (GenCommandLine.CommandLineArgPassed("--no-audio"))
    {
      AudioBackend.Current?.SetOutputPaused(true);
    }
  }

  private static void GenerateTestManifest(string packageId)
  {
    GameObject tempCoroutine = new("TempCoroutine");
    var builder = tempCoroutine.AddComponent<ManifestBuilder>();
    builder.StartCoroutine(Execute(packageId));
    return;

    static IEnumerator Execute(string packageId)
    {
      do
      {
        yield return null;
      } while (LongEventHandler.AnyEventNowOrWaiting ||
               !typeof(Root_Entry).StaticFieldRefAccess<bool>("globalInitDone"));

      try
      {
        ModsConfig.Save();
        ITestManager manager = DevHarmony.GetDevTool<TestFixtureManager>(packageId);
        manager.SaveMetaData();
      }
      catch (Exception ex)
      {
        Log.Error($"Exception thrown while generating manifest: {ex}");
      }
      finally
      {
        // Vanilla QuitAfterBoot waits 3 seconds before exiting for whatever reason. But waiting
        // that long after test discovery is pointless.
        if (GenCommandLine.CommandLineArgPassed("-QuitAfterBoot"))
        {
          Root.Shutdown();
        }
      }
    }
  }

  [UsedImplicitly]
  private class ManifestBuilder : MonoBehaviour
  {
  }
}
