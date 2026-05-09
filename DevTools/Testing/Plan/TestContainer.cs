using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace DevTools.Testing;

internal class TestContainer : ITestFunction, IDisposable
{
  private const int DefaultTimeOut = 600000; // 10 minutes

  private int exitCode = -1;
  private Process process;

  private readonly ModContentPack mod;
  private readonly TestPlanJob job;

  public TestContainer(ModContentPack mod, TestPlanJob job)
  {
    this.mod = mod;
    this.job = job;
  }

  ITestFixture ITestFunction.Fixture => job;

  MethodType ITestFunction.MethodType => MethodType.Test;

  MethodInfo ITestFunction.MethodInfo { get; } = AccessTools.Method(typeof(TestContainer), nameof(ExecuteRoutine));

  object[] ITestCase.Args { get; set; }

  string ITestCase.Name => nameof(TestContainer);

  Type ITestCase.Type => null;

  public Status Status { get; set; }

  public MetaDataContainer MetaData { get; } = new();

  object ITestFunction.ExpectedResult { get; set; }

  // NOTE - Executes synchronously, including during wait time which will block the main thread. This should preferably
  // execute as a sub-routine to allow the UI to update since tests will be conducted in the child-process anyway.
  public void Execute(object _)
  {
    SpawnProcess();

    int timeOutMs = job.timeOut > 0 ? Mathf.RoundToInt(job.timeOut * 1000) : DefaultTimeOut;
    if (!process.WaitForExit(timeOutMs))
    {
      Test.Fail("Timed Out...");
      Kill();
    }
    else
    {
      DevLog.Write($"Finished with exit code {exitCode}");
      Status = exitCode switch
      {
        -1 => Status.NotRun,
        0 => Status.Passed,
        _ => Status.Failed
      };
    }
  }

  public IEnumerator ExecuteRoutine(object _)
  {
    const float ProcessPollInterval = 0.5f;

    SpawnProcess();
    float startTime = Time.realtimeSinceStartup;
    bool timedOut = false;
    while (!process.HasExited)
    {
      if (startTime + job.timeOut >= Time.realtimeSinceStartup)
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
    else
    {
      exitCode = process.ExitCode;
      DevLog.Write($"Finished with exit code {exitCode}");
      Status = exitCode switch
      {
        -1 => Status.NotRun,
        0 => Status.Passed,
        _ => Status.Failed
      };
    }
  }

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
    if (!process.Start())
    {
      Dispose();
      throw new InvalidOperationException("Process was unable to be started.");
    }
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
      else
      {
        exitCode = process.ExitCode;
      }
    }
    catch (Exception ex)
    {
      Test.Fail(ex);
    }
  }

  public void Dispose()
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
}