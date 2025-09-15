using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using LudeonTK;
using RimWorld.Planet;
using UnityEngine.Assertions;
using Verse;
using BenchmarkMethod = System.ValueTuple<System.Type, string, System.Reflection.MethodInfo>;
using Result = DevTools.Benchmarking.Benchmark.Result;

namespace DevTools.Benchmarking;

internal class BenchmarkManager : IDevToolWithMenu
{
	private const string ManagerName = "Benchmark";

	private readonly Dictionary<string, BenchmarkMethods> benchmarks = [];

	string IDevToolWithMenu.Name => ManagerName;

	void IDevToolWithMenu.OpenMenu()
	{
		List<DebugMenuOption> options = [];
		foreach (BenchmarkMethods methods in benchmarks.Values.OrderBy(bm => bm.category))
		{
			if (!IsAllowedGameState(methods.allowedGameStates))
				continue;

			options.Add(new DebugMenuOption(methods.category, DebugMenuOptionMode.Action,
				() => RunBenchmarkFor(methods)));
		}
		Find.WindowStack.Add(new Dialog_DebugOptionListLister(options, ManagerName));
	}

	bool IDevTool.TryRegisterType(Type type)
	{
		BenchmarkClassAttribute classAttr = type.TryGetAttribute<BenchmarkClassAttribute>();
		if (classAttr is null)
			return false;
		string category = classAttr.Category ?? type.FullName;

		// Shouldn't be possible but ReSharper won't shut up so either we do a sanity check
		// or we disable the warning.
		if (category == null)
			throw new NullReferenceException(nameof(category));

		BenchmarkMethods benchmarkMethods = new(category, classAttr);
		benchmarks.TryAdd(category, benchmarkMethods);
		benchmarkMethods.AddFromType(type);
		benchmarkMethods.MetaData.Load(type);
		return true;
	}

	bool IDevTool.Init(ModContentPack mod)
	{
		return true;
	}

	private static void OutputResults(BenchmarkMethods benchmarks,
		List<(string name, Result result)> resultsByMethod)
	{
		GraphType graphType = benchmarks.MetaData.Get(MetaDataName.Graph, GraphType.None);
		if (graphType != GraphType.None)
		{
			//Find.WindowStack.Add(new Dialog_BenchmarkResults(stats, resultsByMethod));
			//return;
		}
		Stat stats = benchmarks.MetaData.Get(MetaDataName.Table, Stat.Mean | Stat.Median | Stat.StdDev | Stat.Samples);
		if (stats > Stat.None)
		{
			Find.WindowStack.Add(
				new Dialog_BenchmarkResults(stats, benchmarks.category, resultsByMethod));
			return;
		}
		StringBuilder stringBuilder = new();
		stringBuilder.AppendLine($"----------     {benchmarks.category}     ----------");
		foreach ((string name, Result result) in resultsByMethod)
		{
			stringBuilder.AppendLine($"{name}: {result.Formatted(result.Mean)}");
		}
		stringBuilder.AppendLine();
		Log.Message(stringBuilder.ToString());
		Log.TryOpenLogWindow();
	}

	private static bool IsAllowedGameState(AllowedGameStates allowedGameStates)
	{
		if (allowedGameStates == AllowedGameStates.Invalid)
			return true;

		bool allowed = false;
		if (allowedGameStates.HasFlag(AllowedGameStates.Entry))
			allowed |= Current.ProgramState == ProgramState.Entry;

		if (allowedGameStates.HasFlag(AllowedGameStates.Playing))
			allowed |= Current.ProgramState == ProgramState.Playing;

		if (allowedGameStates.HasFlag(AllowedGameStates.IsCurrentlyOnMap))
			allowed |= !WorldRendererUtility.WorldRendered && Find.CurrentMap != null;

		if (allowedGameStates.HasFlag(AllowedGameStates.WorldRenderedNow))
			allowed |= WorldRendererUtility.WorldRendered;

		if (allowedGameStates.HasFlag(AllowedGameStates.HasGameCondition))
			allowed |= !WorldRendererUtility.WorldRendered && Find.CurrentMap != null &&
				Find.CurrentMap.gameConditionManager.ActiveConditions.Count > 0;

		return allowed;
	}

	private static void RunBenchmarkFor(BenchmarkMethods benchmarks)
	{
		LongEventHandler.QueueLongEvent(delegate
		{
			InvokeMethods(benchmarks.setupMethods);
			RunBenchmarkMethods(benchmarks);
			InvokeMethods(benchmarks.onFinishMethods);
		}, string.Empty, benchmarks.runAsync, ExceptionHandler);
		return;

		static void ExceptionHandler(Exception ex)
		{
			Log.Error($"Exception thrown running benchmark.\n{ex}");
		}
	}

	private static void InvokeMethods(List<BenchmarkMethod> methods)
	{
		foreach ((Type type, string name, MethodInfo method) in methods)
		{
			LongEventHandler.SetCurrentEventText($"Running {name}");
			ParameterInfo[] parameters = method.GetParameters();
			switch (parameters.Length)
			{
				case 0:
					method.Invoke(null, []);
				break;
				case 1:
					GenGeneric.InvokeStaticGenericMethod(
						typeof(BenchmarkManager),
						parameters[0].ParameterType, nameof(InvokeWithContext), type, method);
				break;
			}
		}
	}

	private static void InvokeWithContext<T>(Type type, MethodInfo method) where T : struct
	{
		method.Invoke(null, [GetContext<T>(type)]);
	}

	private static void RunBenchmarkMethods(BenchmarkMethods benchmarks)
	{
		List<(string, Result)> resultsByMethod = [];
		foreach ((Type type, string name, MethodInfo method) in benchmarks.tests)
		{
			LongEventHandler.SetCurrentEventText($"Running {name}");
			ParameterInfo[] parameters = method.GetParameters();
			Benchmark.Measurement measurement =
				benchmarks.MetaData.Get(MetaDataName.Measurement, Benchmark.Measurement.Auto);
			switch (parameters.Length)
			{
				case 0:
				{
					Result results = method.ReturnType == typeof(void) ?
						RunTest(method, measurement) :
						(Result)GenGeneric.InvokeStaticGenericMethod(typeof(BenchmarkManager),
							method.ReturnType, nameof(RunTestWithReturn), method, measurement);
					resultsByMethod.Add((name, results));
				}
				break;
				case 1:
				{
					Result results = method.ReturnType == typeof(void) ?
						(Result)GenGeneric.InvokeStaticGenericMethod(typeof(BenchmarkManager),
							parameters[0].ParameterType, nameof(RunTestWithContext), type, method, measurement) :
						(Result)AccessTools.Method(typeof(BenchmarkManager), nameof(RunTestWithContextAndReturn))
						 .MakeGenericMethod(parameters[0].ParameterType, method.ReturnType)
						 .Invoke(null, [type, method, measurement]);

					resultsByMethod.Add((name, results));
				}
				break;
			}
		}
		OutputResults(benchmarks, resultsByMethod);
	}

	private static Result RunTest(MethodInfo method, Benchmark.Measurement measurement)
	{
		Assert.IsTrue(method.GetParameters().Length == 0);
		Assert.IsTrue(method.ReturnType == typeof(void));
		return Benchmark.Run(method.MethodHandle.GetFunctionPointer(), measurement);
	}

	private static Result RunTestWithReturn<R>(MethodInfo method, Benchmark.Measurement measurement)
	{
		Assert.IsTrue(method.GetParameters().Length == 0);
		Assert.IsTrue(method.ReturnType == typeof(R));
		return Benchmark.Run<R>(method.MethodHandle.GetFunctionPointer(), measurement);
	}

	private static Result RunTestWithContext<T>(Type declaringType, MethodInfo method,
		Benchmark.Measurement measurement) where T : struct
	{
		Assert.IsTrue(method.GetParameters().Length == 1);
		Assert.IsTrue(method.ReturnType == typeof(void));
		return Benchmark.Run(method.MethodHandle.GetFunctionPointer(), GetContext<T>(declaringType), measurement);
	}

	private static Result RunTestWithContextAndReturn<T, R>(Type declaringType, MethodInfo method,
		Benchmark.Measurement measurement) where T : struct
	{
		Assert.IsTrue(method.GetParameters().Length == 1);
		Assert.IsTrue(method.ReturnType == typeof(R));
		return Benchmark.Run<T, R>(method.MethodHandle.GetFunctionPointer(), GetContext<T>(declaringType), measurement);
	}

	private static T GetContext<T>(Type declaringType) where T : struct
	{
		foreach (PropertyInfo propInfo in declaringType.GetProperties(BindingFlags.Public |
			BindingFlags.NonPublic | BindingFlags.Static))
		{
			ContextAttribute contextAttr = propInfo.TryGetAttribute<ContextAttribute>();
			if (contextAttr is not null && propInfo.CanRead && propInfo.PropertyType == typeof(T))
			{
				return (T)propInfo.GetValue(null);
			}
		}
		foreach (FieldInfo fieldInfo in declaringType.GetFields(BindingFlags.Public |
			BindingFlags.NonPublic | BindingFlags.Static))
		{
			ContextAttribute contextAttr = fieldInfo.TryGetAttribute<ContextAttribute>();
			if (contextAttr is not null && fieldInfo.FieldType == typeof(T))
			{
				return (T)fieldInfo.GetValue(null);
			}
		}
		return new T();
	}

	private class BenchmarkMethods
	{
		public readonly string category;
		public readonly List<BenchmarkMethod> tests = [];
		public readonly List<BenchmarkMethod> setupMethods = [];
		public readonly List<BenchmarkMethod> onFinishMethods = [];
		public readonly bool runAsync;
		public readonly AllowedGameStates allowedGameStates;

		public BenchmarkMethods(string category, BenchmarkClassAttribute classAttr)
		{
			this.category = category;
			this.runAsync = classAttr.RunAsync;
			this.allowedGameStates = classAttr.AllowedGameStates;
		}

		public MetaDataContainer MetaData { get; } = new();

		public void AddFromType(Type type)
		{
			BenchmarkClassAttribute classAttr = type.TryGetAttribute<BenchmarkClassAttribute>();
			if (classAttr.AllowedGameStates != allowedGameStates)
				Log.Error("Mismatched AllowedGameStates property on benchmark categories.");
			if (classAttr.RunAsync != runAsync)
				Log.Error("Mismatched RunAsync setting on benchmark categories.");

			foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
				BindingFlags.Static))
			{
				BenchmarkAttribute benchmarkAttr = method.TryGetAttribute<BenchmarkAttribute>();
				if (benchmarkAttr is not null)
				{
					if (!MethodIsSafe(method, out string reason))
					{
						Log.Error($"Unable to run {method.Name} benchmark. {reason}");
						continue;
					}
					tests.Add((type, benchmarkAttr.Label ?? method.Name, method));
				}
				else if (method.TryGetAttribute<PrepareAttribute>() is not null)
				{
					if (!MethodIsSafe(method, out string reason))
					{
						Log.Error($"Unable to run {method.Name} benchmark. {reason}");
						continue;
					}
					setupMethods.Add((type, method.Name, method));
				}
				else if (method.TryGetAttribute<OnFinishAttribute>() is not null)
				{
					if (!MethodIsSafe(method, out string reason))
					{
						Log.Error($"Unable to run {method.Name} benchmark. {reason}");
						continue;
					}
					onFinishMethods.Add((type, method.Name, method));
				}
			}
		}

		private static bool MethodIsSafe(MethodInfo method, out string reason)
		{
			reason = null;
			if (!method.IsStatic)
			{
				reason = "Method must be static.";
				return false;
			}
			ParameterInfo[] parameters = method.GetParameters();
			if (parameters.Length > 1)
			{
				reason = "Parameter count doesn't match any designated benchmark method.";
				return false;
			}
			if (parameters.Length == 1)
			{
				ParameterInfo pInfo = parameters[0];
				if (!pInfo.ParameterType.IsByRef)
				{
					reason = "Context parameter must be passed by ref.";
					return false;
				}
				if (pInfo.ParameterType.GetElementType() is null or { IsValueType: false })
				{
					reason = "Context type must be a struct.";
					return false;
				}
			}
			return true;
		}
	}
}