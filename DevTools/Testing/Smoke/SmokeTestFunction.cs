using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using DevTools.Benchmarking;
using UnityEngine;
using UnityEngine.Assertions;

namespace DevTools.Testing;

internal class SmokeTestFunction : ITestFunction
{
	private static readonly object[] EmptyArgs = [];

	private readonly MethodInfo method;

	private readonly Stopwatch stopwatch = new();
	private string failMessageInt;

	public SmokeTestFunction(MethodInfo method)
	{
		this.method = method;
	}

	public MethodType MethodType => MethodType.Test;

	public Type Type => method.DeclaringType;

	public Type DeclaringType => MethodInfo.DeclaringType;

	public MetaDataContainer MetaData { get; } = new();

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

	private Exception Exception { get; set; }

	public Benchmark.Result Duration { get; private set; }

	public MethodInfo MethodInfo => method;

	public string Name => method.Name;

	public int TestCount => 1;

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
		Status = Status.NotRun;
		FailLabel = null;
		FailMessage = null;
	}

	public void Execute()
	{
		DevLog.WriteVerbose($"Executing {Type.Name}::{Name}");

		using StopOnDispose sw = new(stopwatch);
		try
		{
			method.Invoke(null, EmptyArgs);
			stopwatch.Stop();
			TabulateTestResults(this);
		}
		catch (TargetInvocationException ex) when (ex.InnerException is AssertionException)
		{
			DevLog.Write(Expect.StatusMessage(Status.Failed, "Exception", ex.InnerException.ToString()));
			FailMessage = ex.InnerException.Message;
			Exception = ex.InnerException;
			Status = Status.Failed;
		}
		catch (Exception ex)
		{
			DevLog.Write(Expect.StatusMessage(Status.Failed, "Exception", ex.ToString()));
			FailMessage = $"{ex.InnerException?.GetType().Name ?? ex.GetType().Name} thrown.";
			Exception = ex;
			Status = Status.Failed;
		}
	}

	public IEnumerator ExecuteRoutine()
	{
		DevLog.WriteVerbose($"Executing {Type.Name}::{Name}");

		Assert.AreEqual(method.ReturnType, typeof(IEnumerator));
		IEnumerator enumerator = (IEnumerator)method.Invoke(null, EmptyArgs);

		using StopOnDispose sw = new(stopwatch);
		while (true)
		{
			object current;
			try
			{
				if (!enumerator.MoveNext())
					break;
				current = enumerator.Current;
			}
			catch (TargetInvocationException ex) when (ex.InnerException is AssertionException)
			{
				DevLog.Write(ex.InnerException.ToString());
				FailMessage = ex.InnerException.Message;
				Exception = ex.InnerException;
				Status = Status.Failed;
				yield break;
			}
			catch (Exception ex)
			{
				DevLog.Write(ex.ToString());
				FailMessage = $"{ex.InnerException?.GetType().Name ?? ex.GetType().Name} thrown.";
				Exception = ex;
				Status = Status.Failed;
				yield break;
			}
			yield return current;
		}
		stopwatch.Stop();
		TabulateTestResults(this);
	}

	private static void TabulateTestResults(SmokeTestFunction function)
	{
		function.Status = Status.Passed;
		function.Duration =
			new Benchmark.Result(function.stopwatch, 1, Benchmark.Measurement.Milliseconds);
	}

	void ITestCase.Fail(string reason)
	{
		Status = Status.Failed;
		FailMessage = reason;
	}

	void ITestCase.TestOutcomes(StatusCount statusCount)
	{
		if (Exception != null)
		{
			switch (Exception)
			{
				case AssertionException:
					statusCount.AssertFailCount++;
				break;
				default:
					statusCount.ExceptionCount++;
				break;
			}
		}
		statusCount.Increment(MethodType.Test, Status);
	}

	void IDataRow<ExplorerColumn>.Draw(Rect rect, ExplorerColumn column)
	{
		column.Draw(rect, this);
	}
}