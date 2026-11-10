using DevTools.Testing;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;

namespace DevTools.Tests;

[TestFixture(TestType.MainMenu)]
[TestDescription("Allows what Core and the DLCs log by themselves, each entry only while its DLC is active.")]
internal class Test_VanillaLogsAllowed
{
  private const string PreceptWarning = "A hidden ritual precept was missing, adding: Funeral (no corpse)";

  [Test]
  private void HiddenRitualPrecept()
  {
    Assert.AreEqual(ModsConfig.IdeologyActive, LogWatcher.VanillaLogAllowed(LogType.Warning, PreceptWarning));
    Assert.IsFalse(LogWatcher.VanillaLogAllowed(LogType.Error, PreceptWarning));
    Assert.IsFalse(LogWatcher.VanillaLogAllowed(LogType.Warning, $"Unrelated: {PreceptWarning}"));
  }

  [Test, LoadIfModsActive("Ludeon.RimWorld.Ideology")]
  private void LogWatcherAllowsVanillaLogs()
  {
    LogWatcher.TestLogEntry(new TestConfig(), LogType.Warning, new LogWatcher.LogEntry(PreceptWarning, string.Empty));
  }
}
