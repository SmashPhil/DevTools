using System;
using System.Collections.Generic;
using System.Diagnostics;
using DevTools.Benchmarking;
using DevTools.Testing.Plan;
using UnityEngine;
using Verse;

namespace DevTools.Testing;

internal class TestPlanJob : ITestGroup
{
	public string name;
	public string commandLineArgs;
	public List<string> loadWithMods;
	public float timeOut = -1;

	[Unsaved]
	private TestContainer container;

	[Unsaved]
	private readonly Stopwatch stopwatch = new();

	[Unsaved]
	private string failMessageInt;

	public TestType TestType => TestType.MainMenu;

	public string SaveFile => null;

	public IEnumerable<ITestFunction> TestFunctions
	{
		get { yield return container; }
	}

	public string Name => name;

	public Type Type => typeof(TestPlanJob);

	public int TestCount => 1;

	public MetaDataContainer MetaData { get; } = new();

	public Benchmark.Result Duration { get; private set; }

	public Status Status { get; set; } = Status.NotRun;

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

	bool IDataRow<ExplorerColumn>.ShouldHide => MetaData.Get<bool>(MetaDataName.Disabled);

	bool IDataRow<ExplorerColumn>.CanExpand => TestCount > 1;

	float IDataRow<ExplorerColumn>.Height => ExplorerColumn.LineHeight;

	bool IDataRow<ExplorerColumn>.Expanded { get; set; }

	IEnumerable<IDataRow<ExplorerColumn>> IDataRow<ExplorerColumn>.NestedRows
	{
		get { yield return container; }
	}

	public void PostLoadInit(ModContentPack mod)
	{
		container = new TestContainer(mod, this);
	}

	void IDataRow<ExplorerColumn>.Draw(Rect rect, ExplorerColumn column)
	{
		column.Draw(rect, this);
	}

	void ITestCase.Fail(string reason)
	{
		Status = Status.Failed;
		FailMessage = reason;
	}

	void ITestCase.Reset()
	{
		Status = Status.NotRun;
		container.Reset();
	}

	bool ITestGroup.SetUp()
	{
		stopwatch.Restart();
		return true;
	}

	bool ITestGroup.TearDown()
	{
		stopwatch.Stop();
		Duration = new Benchmark.Result(stopwatch, 1, Benchmark.Measurement.Milliseconds);
		container.Dispose();
		Status = container.Status;
		return true;
	}

	void ITestCase.TestOutcomes(StatusCount statusCount)
	{
		container.TestOutcomes(statusCount);
	}

	public int CompareTo(ITestGroup other)
	{
		return 0;
	}
}