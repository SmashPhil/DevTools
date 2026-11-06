using System.Collections;
using System.Diagnostics;
using System.IO;
using DevTools.Testing;
using UnityEngine.Assertions;
using Verse;

namespace DevTools.Tests;

[TestFixture(TestType.Playing)]
[WarningsAllowed("A hidden ritual precept was missing")]
internal class Test_SaveLoad : IWorldGeneration
{
  private const string SaveName = "DevTools_Test_SaveLoad";
  private const int LoadTimeoutSeconds = 120;

  // Loading replaces the game, so the fixture after this one needs a new one.
  WorldGenerationSettings IWorldGeneration.WorldGenerationSettings => new();

  [OneTimeTearDown]
  private void DeleteSave()
  {
    File.Delete(GenFilePaths.FilePathForSavedGame(SaveName));
  }

  [Test]
  private IEnumerator DefaultTestMap()
  {
    Assert.IsNotNull(DefDatabase<MapGeneratorDef>.GetNamedSilentFail(Find.CurrentMap.generatorDef.defName));
    int startingPawns = Find.GameInfo.startingAndOptionalPawns.Count;

    GameDataSaveLoader.SaveGame(SaveName);
    GameDataSaveLoader.LoadGame(SaveName);

    Stopwatch loading = Stopwatch.StartNew();
    while (Verse.Current.ProgramState != ProgramState.Playing || LongEventHandler.AnyEventNowOrWaiting)
    {
      Assert.IsTrue(loading.Elapsed.TotalSeconds < LoadTimeoutSeconds, "Timed out loading the save.");
      yield return null;
    }

    Assert.IsNotNull(Find.CurrentMap.generatorDef);
    Assert.AreEqual(startingPawns, Find.GameInfo.startingAndOptionalPawns.Count);
  }
}
