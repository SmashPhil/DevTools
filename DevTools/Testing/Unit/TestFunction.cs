using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DevTools.Benchmarking;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;

namespace DevTools.Testing;

[PublicAPI]
public class TestFunction : ITestFunction
{
	private static readonly object[] EmptyArgs = [];

	private readonly object instance;
	private readonly MethodInfo method;

	public TestFunction(object instance, MethodInfo method, MethodType methodType)
	{
		this.instance = instance;
		this.method = method;
		MethodType = methodType;
	}

	public MethodType MethodType { get; }

	public Type Type => method.DeclaringType;

	public MetaDataContainer MetaData { get; } = new();

	internal ContextGroup Root { get; private set; } = new(null);

	public Status Status
	{
		get => Root.Status;
		set => Root.Status = value;
	}

	public string FailLabel => Root.FailLabel;

	public string FailMessage => Root.FailMessage;

	public Benchmark.Result Duration => Root.Duration;

	public MethodInfo MethodInfo => method;

	public string Name => method.Name;

	public int TestCount => Root.TestCount;

	bool IDataRow<ExplorerColumn>.ShouldHide => MetaData.Get<bool>(MetaDataName.Disabled);

	bool IDataRow<ExplorerColumn>.CanExpand => Root.CanExpand;

	bool IDataRow<ExplorerColumn>.Expanded { get; set; }

	float IDataRow<ExplorerColumn>.Height => ExplorerColumn.LineHeight;

	IEnumerable<IDataRow<ExplorerColumn>> IDataRow<ExplorerColumn>.NestedRows
	{
		get
		{
			foreach (ContextGroup group in Root.Groups)
				yield return group;
		}
	}

	public void Reset()
	{
		Root.Reset();
	}

	public void Execute()
	{
		DevLog.WriteVerbose($"Executing {Type.Name}::{Name}");
		Root.Reset();

		// Empty group to capture test results at the root level
		using Test.Group group = new(null);
		Root = Test.CurrentGroup;
		Root.Function = this;
		try
		{
			method.Invoke(instance, EmptyArgs);
			ContextGroup.TabulateTestResultsRecursive(Root);
		}
		catch (TargetInvocationException ex) when (ex.InnerException is AssertionException)
		{
			DevLog.Write(Expect.StatusMessage(Status.Failed, "Exception", ex.InnerException.ToString()));
			Test.CurrentGroup.FailMessage = ex.InnerException.Message;
			Test.CurrentGroup.Exception = ex.InnerException;
			Test.CurrentGroup.Status = Status.Failed;
		}
		catch (Exception ex)
		{
			DevLog.Write(Expect.StatusMessage(Status.Failed, "Exception", ex.ToString()));
			Test.CurrentGroup.FailMessage =
				$"{ex.InnerException?.GetType().Name ?? ex.GetType().Name} thrown.";
			Test.CurrentGroup.Exception = ex;
			Test.CurrentGroup.Status = Status.Failed;
		}
	}

	public IEnumerator ExecuteRoutine()
	{
		DevLog.WriteVerbose($"Executing {Type.Name}::{Name}");
		Root.Reset();

		// Empty group to capture test results at the root level
		using Test.Group group = new(null);
		Root = Test.CurrentGroup;
		Root.Function = this;

		Assert.AreEqual(method.ReturnType, typeof(IEnumerator));
		IEnumerator enumerator = (IEnumerator)method.Invoke(instance, EmptyArgs);

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
				Test.CurrentGroup.FailMessage = ex.InnerException.Message;
				Test.CurrentGroup.Exception = ex.InnerException;
				Test.CurrentGroup.Status = Status.Failed;
				yield break;
			}
			catch (Exception ex)
			{
				DevLog.Write(ex.ToString());
				Test.CurrentGroup.FailMessage =
					$"{ex.InnerException?.GetType().Name ?? ex.GetType().Name} thrown.";
				Test.CurrentGroup.Exception = ex;
				Test.CurrentGroup.Status = Status.Failed;
				yield break;
			}
			yield return current;
		}
		ContextGroup.TabulateTestResultsRecursive(Root);
	}

	void ITestCase.Fail(string reason)
	{
		Root?.Fail(reason);
	}

	void ITestCase.TestOutcomes(StatusCount statusCount)
	{
		Root?.TestOutcomes(statusCount);
	}

	void IDataRow<ExplorerColumn>.Draw(Rect rect, ExplorerColumn column)
	{
		column.Draw(rect, this);
	}
}