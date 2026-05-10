using System;
using System.Collections;
using System.Diagnostics;
using System.Text;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;

namespace DevTools.Testing;

// Sole test fixture for test plans. It kicks off a process which will load and run the plan's tests
// separately, while this fixture waits for the process to exit.
internal sealed class TestProcess
{
  private const int DefaultTimeOut = 600000; // 10 minutes

  private Process process;
  private bool timedOut;

  public ModContentPack mod;
  public TestPlanJob job;

  [OneTimeSetUp]
  private void SpawnProcess()
  {
    if (process != null)
      return;

    if (!job.loadWithMods.NullOrEmpty())
    {
      if (!job.loadWithMods.Contains(mod.PackageId) &&
          !job.loadWithMods.Contains(mod.ModMetaData.PackageIdNonUnique.ToLowerInvariant()))
      {
        Test.Fail($"Loading mod list without plan runner {mod.Name} loaded. This is not supported.");
        return;
      }
      ModsConfig.SaveFromList(job.loadWithMods);
    }
    string fileName = Environment.GetCommandLineArgs()[0];
    StringBuilder claBuilder = new();
    claBuilder.Append($"--pid \"{mod.PackageId}\" -e"); // -batchmode
    if (!job.commandLineArgs.NullOrEmpty())
    {
      claBuilder.Append($" {job.commandLineArgs}");
    }

    process = new Process();
    process.StartInfo.FileName = fileName;
    process.StartInfo.Arguments = claBuilder.ToString();
    process.StartInfo.CreateNoWindow = false;
    process.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
    Assert.IsTrue(process.Start(), "Process was unable to be started.");
  }

  [OneTimeTearDown]
  private void DisposeProcess()
  {
    if (process is { HasExited: false })
    {
      // If process has to be killed on disposal, it's considered a failure. It should've properly terminated
      // at the end of its own test runner.
      Test.Fail("Killing process before it was able to finish.");
      Kill();
    }
    process?.Dispose();
    process = null;
  }

  [Test, HideInUI]
  private IEnumerator WaitForProcess()
  {
    const float ProcessPollInterval = 0.5f;

    float startTime = Time.realtimeSinceStartup;
    float timeOut = job.timeOut > 0 ? job.timeOut : DefaultTimeOut;

    while (!process.HasExited)
    {
      if (Time.realtimeSinceStartup >= startTime + timeOut)
      {
        timedOut = true;
        break;
      }
      yield return new WaitForSecondsRealtime(ProcessPollInterval);
    }
    if (timedOut || !process.HasExited)
    {
      Test.Fail("Timed Out...");
      Kill();
    }
    int exitCode = process.ExitCode;
    DevLog.Write($"Finished with exit code {exitCode}");
    Expect.AreEqual(expected: 0, exitCode, $"Process exited with code {exitCode}");
  }

  private void Kill()
  {
    if (process is null || process.HasExited)
      return;

    const int KillTimeOutMs = 2000; // ms

    try
    {
      process.Kill();
      if (!process.WaitForExit(KillTimeOutMs))
      {
        Test.Fail("Failed to kill process, it may be orphaned.");
      }
    }
    catch (Exception ex)
    {
      Test.Fail(ex);
    }
  }
}
