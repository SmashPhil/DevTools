using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DevTools.Benchmarking;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;

namespace DevTools.Testing;

internal class ContextGroup : ITestCase
{
	private readonly Stopwatch stopwatch = new();

	private string failMessageInt;

	public ContextGroup(string label, ContextGroup parent = null)
	{
		Name = label;
		Parent = parent;
		Function = parent?.Function; // Root (null parent) will have TestCase self assign
	}

	public string Name { get; }

	public Type Type => Parent?.Type;

	public int TestCount => Results.Count + Groups.Sum(group => group.TestCount);

	public Dictionary<string, string> Traits { get; } = [];

	public TestFunction Function { get; set; }

	public ContextGroup Parent { get; }

	public MetaDataContainer MetaData { get; } = new();

	public List<ContextGroup> Groups { get; } = [];

	public List<TestResult> Results { get; } = [];

	bool IDataRow<ExplorerColumn>.ShouldHide => MetaData.Get<bool>(MetaDataName.Disabled);

	public bool CanExpand => !Groups.NullOrEmpty();

	bool IDataRow<ExplorerColumn>.Expanded { get; set; }

	float IDataRow<ExplorerColumn>.Height => ExplorerColumn.LineHeight;

	IEnumerable<IDataRow<ExplorerColumn>> IDataRow<ExplorerColumn>.NestedRows => Groups;

	public Benchmark.Result Duration { get; private set; }

	public Status Status { get; set; } = Status.NotRun;

	public string FailLabel { get; private set; }

	public string FailMessage
	{
		get { return failMessageInt; }
		set
		{
			failMessageInt = value;
			FailLabel = FailMessage.FirstLine();
		}
	}

	public Exception Exception { get; set; }

	public void Open()
	{
		stopwatch.Restart();
	}

	public void Close()
	{
		stopwatch.Stop();
	}

	public void Reset()
	{
		ResetRecursive(this);
		return;

		static void ResetRecursive(ContextGroup group)
		{
			group.Status = Status.NotRun;
			group.FailMessage = null;
			group.Results.Clear();
			foreach (ContextGroup subGroup in group.Groups)
			{
				ResetRecursive(subGroup);
			}
		}
	}

	public static void TabulateTestResultsRecursive(ContextGroup group)
	{
		group.Status = Status.Passed;
		group.Duration =
			new Benchmark.Result(group.stopwatch, 1, Benchmark.Measurement.Milliseconds);
		foreach (TestResult testResult in group.Results)
		{
			// We only care about the worst offender, we'll be outputting the full result
			// list and all Expect cases for verbose debug output anyways.
			if (testResult.status >= group.Status)
				continue;

			switch (testResult.status)
			{
				case Status.Failed:
					group.Status = testResult.status;
					group.FailMessage = $"{testResult.label} failed.";
				break;
				case Status.Canceled or Status.Skipped:
					// Reason for skip or cancellation will be in the message.
					group.Status = testResult.status;
					group.FailMessage = testResult.message;
				break;
			}
		}
		foreach (ContextGroup contextGroup in group.Groups)
		{
			TabulateTestResultsRecursive(contextGroup);
			// Propagate failure to the top
			if (contextGroup.Status < group.Status)
				group.Status = contextGroup.Status;
		}
	}

	void IDataRow<ExplorerColumn>.Draw(Rect rect, ExplorerColumn column)
	{
		column.Draw(rect, this);
	}

	public void Fail(string reason)
	{
		Status = Status.Failed;
		FailMessage = reason;
	}

	public void TestOutcomes(StatusCount statusCount)
	{
		SumResultCount(this, statusCount);
	}

	private static void SumResultCount(ContextGroup group, StatusCount statusCount)
	{
		if (group.Exception != null)
		{
			switch (group.Exception)
			{
				case AssertionException:
					statusCount.AssertFailCount++;
				break;
				default:
					statusCount.ExceptionCount++;
				break;
			}
		}
		foreach (TestResult testResult in group.Results)
		{
			statusCount.Increment(group.Function.MethodType, testResult.status);
		}
		foreach (ContextGroup subGroup in group.Groups)
		{
			SumResultCount(subGroup, statusCount);
		}
	}
}