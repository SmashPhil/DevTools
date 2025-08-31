using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using JetBrains.Annotations;
using UnityEngine;
using Verse;

namespace DevTools.Testing;

[PublicAPI]
public class TestPlan
{
	public string name;

	private bool continueOnFailure = true;
	private List<Step> steps;

	private Logger.Config log = new();

	internal void Start(ModContentPack mod)
	{
		if (steps.NullOrEmpty())
		{
			Log.Error("Trying to run test plan with no steps.");
			return;
		}
		CoroutineObject.Instance.StartCoroutine(TestRoutine(mod));
	}

	private IEnumerator TestRoutine(ModContentPack mod)
	{
		const float ProcessPollInterval = 0.5f;

		Application.runInBackground = true;
		using DevLog.Enabler dve = new(log);

		List<string> activeMods = [];
		ModsConfig.SetActiveToList(activeMods);
		Data data = new(steps.Count);
		while (data.Current < steps.Count)
		{
			Step step = steps[data.Current];
			float startTime = Time.realtimeSinceStartup;
			using ProcessContainer process = new(mod, step);
			while (!process.HasExited && startTime + step.timeOut < Time.realtimeSinceStartup)
			{
				yield return new WaitForSecondsRealtime(ProcessPollInterval);
			}
			data.Record(process.ExitCode);
			if (data.AnyFailed && !continueOnFailure)
				break;
		}
		// Restore mods config to current mod list
		ModsConfig.SaveFromList(activeMods);
		// TODO - Finalize results
	}

	[PublicAPI]
	private class Step
	{
		public string name;
		public string commandLineArgs = "--smoke-test";
		public List<string> loadWithMods;
		public float timeOut = -1;
	}

	private class Data
	{
		private int current;

		public readonly Status[] results;

		public Data(int steps)
		{
			results = new Status[steps];
		}

		public int Current => current;

		public bool AnyFailed { get; private set; }

		public void Record(int result)
		{
			AnyFailed |= result != 0;
			results[Current] = result == 0 ? Status.Passed : Status.Failed;
			current++;
		}
	}

	private class ProcessContainer(ModContentPack mod, Step step) : IDisposable
	{
		private int? exitCode;
		private Process process;

		public int ExitCode => exitCode ?? (HasExited ? process.ExitCode : 0);

		public bool HasExited => process is null or { HasExited: true };

		public void Start()
		{
			if (process != null)
				return;

			if (!step.loadWithMods.NullOrEmpty())
			{
				if (!step.loadWithMods.Contains(mod.PackageId) &&
					!step.loadWithMods.Contains(mod.ModMetaData.PackageIdNonUnique.ToLowerInvariant()))
				{
					Log.Error($"Loading mod list without plan runner {mod.Name} loaded This is not supported.");
					exitCode = 1;
					return;
				}
				ModsConfig.SaveFromList(step.loadWithMods);
			}
			string fileName = Environment.GetCommandLineArgs()[0];
			StringBuilder claBuilder = new();
			claBuilder.Append($"--pid \"{mod.PackageId}\""); // -batchmode -nographics
			if (!step.commandLineArgs.NullOrEmpty())
			{
				claBuilder.Append($" {step.commandLineArgs}");
			}

			process = new Process();
			process.StartInfo.FileName = fileName;
			process.StartInfo.Arguments = claBuilder.ToString();
			if (!process.Start())
			{
				Log.Error($"Failed to start process {fileName}.");
				exitCode = 1;
				return;
			}
			const int DefaultTimeOut = 600000; // 10 minutes
			int timeOutMs = step.timeOut > 0 ? Mathf.RoundToInt(step.timeOut * 1000) : DefaultTimeOut;
			if (!process.WaitForExit(timeOutMs))
			{
				DevLog.Write("Process timed out. Failing...");
				Kill();
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
					DevLog.Write("Failed to kill process, it may be orphaned.");
				}
			}
			catch (Exception ex)
			{
				DevLog.Write($"Exception thrown killing process!\n{ex}");
			}
		}

		void IDisposable.Dispose()
		{
			if (process is { HasExited: false })
			{
				exitCode = 1;
				Kill();
			}
			process?.Dispose();
			process = null;
		}
	}
}