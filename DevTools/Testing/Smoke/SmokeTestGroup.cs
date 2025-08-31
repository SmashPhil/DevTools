using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using DevTools.Benchmarking;
using UnityEngine;
using Verse;

namespace DevTools.Testing;

internal class SmokeTestGroup : ITestGroup
{
	private readonly List<SmokeTestFunction> tests = [];

	private string failMessageInt;
	private readonly Stopwatch groupTimer = new();

	public SmokeTestGroup(Type type, TestType testType)
	{
		Type = type;
		TestType = testType;
	}

	public string Name => TestType.ToString();

	public int TestCount => tests.Count;

	string ITestGroup.SaveFile => null;

	public IEnumerable<ITestFunction> TestFunctions => tests;

	public MetaDataContainer MetaData { get; } = new();

	public TestType TestType { get; }

	public Type Type { get; }

	public Benchmark.Result Duration { get; private set; }

	public Status Status { get; private set; } = Status.NotRun;

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
		get
		{
			foreach (SmokeTestFunction method in tests)
			{
				yield return method;
			}
		}
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
		foreach (SmokeTestFunction function in tests)
		{
			function.Reset();
		}
	}

	bool ITestGroup.SetUp()
	{
		groupTimer.Restart();

		// TODO - prep map

		return true;
	}

	bool ITestGroup.TearDown()
	{
		groupTimer.Stop();
		Duration = new Benchmark.Result(groupTimer, 1, Benchmark.Measurement.Milliseconds);
		Status = tests.Min(test => test.Status);

		// TODO - prep map

		return true;
	}

	void ITestCase.TestOutcomes(StatusCount statusCount)
	{
		foreach (ITestCase testCase in tests)
			testCase.TestOutcomes(statusCount);
	}

	public bool TryAddFunction(MethodInfo methodInfo)
	{
		if (!MethodIsSafe(methodInfo, out string reason))
		{
			Log.Error($"Unable to add {methodInfo.Name} to smoke test. {reason}");
			return false;
		}

		if (!methodInfo.HasAllRequiredMods() || !methodInfo.HasAnyRequiredMods())
			return false;

		SmokeTestFunction method = new(methodInfo);
		method.MetaData.Load(methodInfo);
		tests.Add(method);
		return true;
	}

	private static bool MethodIsSafe(MethodInfo method, out string reason)
	{
		reason = null;
		if (method.ReturnType != typeof(void) && method.ReturnType != typeof(IEnumerator))
		{
			reason = "Return type must be IEnumerator or void.";
			return false;
		}
		ParameterInfo[] parameters = method.GetParameters();
		if (parameters.Length > 0)
		{
			reason = "Smoke test methods must not have any parameters";
			return false;
		}
		return true;
	}

	int IComparable<ITestGroup>.CompareTo(ITestGroup other)
	{
		if (other is null)
			return -1;
		return TestType.CompareTo(other.TestType);
	}
}