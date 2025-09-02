using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using DevTools.Benchmarking;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;

namespace DevTools.Testing.Plan;

internal class TestContainer : ITestFunction, IDisposable
{
	private const int DefaultTimeOut = 600000; // 10 minutes

	private int exitCode = -1;
	private Process process;

	private readonly ModContentPack mod;
	private readonly TestPlanJob job;

	private string failMessageInt;
	private readonly Stopwatch stopwatch = new();

	public TestContainer(ModContentPack mod, TestPlanJob job)
	{
		this.mod = mod;
		this.job = job;
	}

	private bool HasExited => process is null or { HasExited: true };

	MethodType ITestFunction.MethodType => MethodType.Test;

	MethodInfo ITestFunction.MethodInfo { get; } = AccessTools.Method(typeof(TestContainer), nameof(ExecuteRoutine));

	string ITestCase.Name => nameof(TestContainer);

	Type ITestCase.Type => GetType();

	int ITestCase.TestCount => 1;

	public Benchmark.Result Duration { get; private set; }

	public Status Status { get; set; }

	public string FailLabel { get; private set; }

	public string FailMessage
	{
		get { return failMessageInt; }
		private set
		{
			failMessageInt = value;
			FailLabel = FailMessage.FirstLine();
		}
	}

	public MetaDataContainer MetaData { get; } = new();

	bool IDataRow<ExplorerColumn>.ShouldHide => MetaData.Get<bool>(MetaDataName.Disabled);

	bool IDataRow<ExplorerColumn>.CanExpand => false;

	bool IDataRow<ExplorerColumn>.Expanded { get; set; }

	float IDataRow<ExplorerColumn>.Height => ExplorerColumn.LineHeight;

	IEnumerable<IDataRow<ExplorerColumn>> IDataRow<ExplorerColumn>.NestedRows
	{
		get { yield return this; }
	}

	public void Reset()
	{
		exitCode = -1;
		Status = Status.NotRun;
		FailLabel = null;
		FailMessage = null;
	}

	public void Fail(string reason)
	{
		Status = Status.Failed;
		FailMessage = reason;
		DevLog.Write($"{Expect.FailedLabel} {reason}");
	}

	public void TestOutcomes(StatusCount statusCount)
	{
		statusCount.Increment(MethodType.Test, Status);
	}

	void IDataRow<ExplorerColumn>.Draw(Rect rect, ExplorerColumn column)
	{
		column.Draw(rect, this);
	}

	// NOTE - Executes synchronously, including during wait time which will block the main thread. This should preferably
	// execute as a sub-routine to allow the UI to update since tests will be conducted in the child-process anyways.
	public void Execute()
	{
		stopwatch.Restart();
		SpawnProcess();

		int timeOutMs = job.timeOut > 0 ? Mathf.RoundToInt(job.timeOut * 1000) : DefaultTimeOut;
		if (!process.WaitForExit(timeOutMs))
		{
			Fail("Timed Out...");
			Kill();
		}
		else
		{
			DevLog.Write($"Finished with exit code {exitCode}");
			Status = exitCode switch
			{
				-1 => Status.NotRun,
				0  => Status.Passed,
				_  => Status.Failed
			};
		}
		stopwatch.Stop();
		Duration = new Benchmark.Result(stopwatch, 1, Benchmark.Measurement.Milliseconds);
	}

	public IEnumerator ExecuteRoutine()
	{
		const float ProcessPollInterval = 0.5f;

		stopwatch.Restart();
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
			Fail("Timed Out...");
			Kill();
		}
		else
		{
			exitCode = process.ExitCode;
			DevLog.Write($"Finished with exit code {exitCode}");
			Status = exitCode switch
			{
				-1 => Status.NotRun,
				0  => Status.Passed,
				_  => Status.Failed
			};
		}
		stopwatch.Stop();
		Duration = new Benchmark.Result(stopwatch, 1, Benchmark.Measurement.Milliseconds);
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
				Fail($"Loading mod list without plan runner {mod.Name} loaded. This is not supported.");
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
				Fail("Failed to kill process, it may be orphaned.");
			}
			else
			{
				exitCode = process.ExitCode;
			}
		}
		catch (Exception ex)
		{
			DevLog.Write($"Exception thrown killing process!\n{ex}");
			Fail("Exception thrown killing process.");
		}
	}

	public void Dispose()
	{
		if (process is { HasExited: false })
		{
			// If process has to be killed on disposal, it's considered a failure. It should've properly terminated
			// at the end of its own test runner.
			Fail("Killing process before it was able to finish.");
			Kill();
		}
		process?.Dispose();
		process = null;
	}
}