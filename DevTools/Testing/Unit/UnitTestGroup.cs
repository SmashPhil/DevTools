using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using DevTools.Benchmarking;
using JetBrains.Annotations;
using UnityEngine;
using Verse;

namespace DevTools.Testing;

[PublicAPI]
internal class UnitTestGroup : ITestGroup
{
	private readonly List<TestFunction> setUps = [];
	private readonly List<TestFunction> tests = [];
	private readonly List<TestFunction> tearDowns = [];

	private readonly ITestManager testManager;

	private string failMessageInt;
	private readonly Stopwatch groupTimer = new();

	private readonly Dictionary<Type, object> instanceByType = [];

	public UnitTestGroup(ITestManager testManager, Type type, TestType testType)
	{
		this.testManager = testManager;
		TestType = testType;
		Type = type;
	}

	string ITestGroup.SaveFile => MetaData.Get<string>(MetaDataName.LoadSave);

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
		set
		{
			failMessageInt = value;
			FailLabel = FailMessage.FirstLine();
		}
	}

	public int TestCount => tests.Count;

	public string Name => Type.Name;

	bool IDataRow<ExplorerColumn>.ShouldHide => MetaData.Get<bool>(MetaDataName.Disabled);

	bool IDataRow<ExplorerColumn>.CanExpand =>
		TestCount > 1 || (TestCount == 1 && tests.Any(method => method.Root.CanExpand));

	float IDataRow<ExplorerColumn>.Height => ExplorerColumn.LineHeight;

	bool IDataRow<ExplorerColumn>.Expanded { get; set; }

	IEnumerable<IDataRow<ExplorerColumn>> IDataRow<ExplorerColumn>.NestedRows
	{
		get
		{
			foreach (TestFunction method in setUps)
			{
				if (method.Status != Status.Passed && method.Status != Status.NotRun)
					yield return method;
			}
			foreach (TestFunction method in tests)
			{
				yield return method;
			}
			foreach (TestFunction method in tearDowns)
			{
				if (method.Status != Status.Passed && method.Status != Status.NotRun)
					yield return method;
			}
		}
	}

	public void Reset()
	{
		Status = Status.NotRun;

		foreach (TestFunction testFunction in setUps)
			testFunction.Reset();
		foreach (TestFunction testFunction in tests)
			testFunction.Reset();
		foreach (TestFunction testFunction in tearDowns)
			testFunction.Reset();
	}

	bool ITestGroup.SetUp()
	{
		groupTimer.Restart();

		bool success = true;
		foreach (TestFunction function in setUps)
		{
			if (function.IsDisabled())
				continue;
			using LogWatcher lw = new(testManager.Config, function);
			function.Execute();
			success &= function.Status == Status.Passed;
		}
		return success;
	}

	bool ITestGroup.TearDown()
	{
		try
		{
			bool success = true;
			foreach (TestFunction function in tearDowns)
			{
				if (function.IsDisabled())
					continue;
				using LogWatcher lw = new(testManager.Config, function);
				function.Execute();
				success &= function.Status == Status.Passed;
			}
			return success;
		}
		finally
		{
			groupTimer.Stop();
			Duration = new Benchmark.Result(groupTimer, 1, Benchmark.Measurement.Milliseconds);
			Status = setUps.Concat(tests).Concat(tearDowns).Min(test => test.Status);
		}
	}

	public void AddFromType(Type type)
	{
		foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
			BindingFlags.Static | BindingFlags.Instance))
		{
			TryAddMethod<SetUpAttribute>(type, method, MethodType.SetUp, setUps);
			TryAddMethod<TestAttribute>(type, method, MethodType.Test, tests);
			TryAddMethod<TearDownAttribute>(type, method, MethodType.TearDown, tearDowns);
		}
		return;

		void TryAddMethod<T>(Type declaringType, MethodInfo methodInfo, MethodType methodType,
			List<TestFunction> methodList) where T : Attribute
		{
			if (methodInfo.TryGetAttribute<T>() is not null)
			{
				if (!MethodIsSafe(methodInfo, out string reason))
				{
					Log.Error($"Unable to add {methodInfo.Name} to unit test. {reason}");
					return;
				}

				if (!methodInfo.HasAllRequiredMods() || !methodInfo.HasAnyRequiredMods())
					return;

				object instance = null;
				// Static types are both abstract and sealed
				if (declaringType.IsAbstract)
				{
					// Only static types should be getting added as a unit test method if the type
					// is abstract, otherwise we wouldn't be able to invoke the method.
					if (!declaringType.IsSealed)
						Log.Error("Trying to instantiate abstract type for unit testing.");
				}
				else
				{
					if (!instanceByType.TryGetValue(declaringType, out instance))
					{
						instance = Activator.CreateInstance(declaringType);
						instanceByType[declaringType] = instance;
					}
				}
				TestFunction method = new(instance, methodInfo, methodType);
				method.MetaData.Load(methodInfo);
				methodList.Add(method);
			}
		}
	}

	public void SortByExecutionPriority()
	{
		setUps.Sort(TestCaseComparer.Default);
		tests.Sort(TestCaseComparer.Default);
		tearDowns.Sort(TestCaseComparer.Default);
	}

	private static bool MethodIsSafe(MethodInfo method, out string reason)
	{
		reason = null;
		if (method.ReturnType != typeof(void))
		{
			if (method.HasAttribute<SetUpAttribute>() ||
				method.HasAttribute<TearDownAttribute>())
			{
				reason = "Return type must be void.";
				return false;
			}
			if (method.ReturnType != typeof(IEnumerator))
			{
				reason = "Return type must be IEnumerator or void.";
				return false;
			}
		}
		ParameterInfo[] parameters = method.GetParameters();
		if (parameters.Length > 0)
		{
			reason = "Test methods must not have any parameters.";
			return false;
		}
		return true;
	}

	void ITestCase.Fail(string reason)
	{
		Status = Status.Failed;
		FailMessage = reason;
	}

	void ITestCase.TestOutcomes(StatusCount statusCount)
	{
		foreach (ITestCase testCase in setUps)
			testCase.TestOutcomes(statusCount);
		foreach (ITestCase testCase in tests)
			testCase.TestOutcomes(statusCount);
		foreach (ITestCase testCase in tearDowns)
			testCase.TestOutcomes(statusCount);
	}

	void IDataRow<ExplorerColumn>.Draw(Rect rect, ExplorerColumn column)
	{
		column.Draw(rect, this);
	}

	int IComparable<ITestGroup>.CompareTo(ITestGroup other)
	{
		if (other is null)
			return -1;
		return TestType.CompareTo(other.TestType);
	}
}