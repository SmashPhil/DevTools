using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using LudeonTK;
using RimWorld.Planet;
using UnityEngine.Assertions;
using Verse;
using BenchmarkMethod = System.ValueTuple<string, System.Reflection.MethodInfo>;
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
      if (!IsAllowedGameState(methods.AllowedGameStates))
        continue;

      options.Add(new DebugMenuOption(methods.category, DebugMenuOptionMode.Action, methods.Run));
    }
    Find.WindowStack.Add(new Dialog_DebugOptionListLister(options, ManagerName));
  }

  bool IDevTool.TryRegisterType(Type type)
  {
    BenchmarkClassAttribute classAttr = type.TryGetAttribute<BenchmarkClassAttribute>();
    if (classAttr is null)
      return false;
    string category = classAttr.Category ?? type.FullName;
    if (benchmarks.ContainsKey(category!))
    {
      Log.Error($"Trying to register duplicate benchmark {category}.");
      return false;
    }
    BenchmarkMethods benchmark = new(category, type, classAttr);
    if (!benchmark.HasTests)
      return false;

    benchmarks.Add(category, benchmark);
    benchmark.MetaData.Load(type);
    return true;
  }

  bool IDevTool.Init(ModContentPack mod)
  {
    if (!Stopwatch.IsHighResolution)
    {
      Log.WarningOnce("Stopwatch is not high resolution on this device.", "StopwatchHighFreq".GetHashCode());
      return false;
    }
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
    if ((allowedGameStates & AllowedGameStates.Entry) != 0)
    {
      allowed |= Current.ProgramState == ProgramState.Entry;
    }

    if ((allowedGameStates & AllowedGameStates.Playing) != 0)
    {
      allowed |= Current.ProgramState == ProgramState.Playing;
    }

    if ((allowedGameStates & AllowedGameStates.IsCurrentlyOnMap) != 0)
    {
      allowed |= !WorldRendererUtility.WorldRendered && Find.CurrentMap != null;
    }

    if ((allowedGameStates & AllowedGameStates.WorldRenderedNow) != 0)
    {
      allowed |= WorldRendererUtility.WorldRendered;
    }

    if ((allowedGameStates & AllowedGameStates.HasGameCondition) != 0)
    {
      allowed |= !WorldRendererUtility.WorldRendered && Find.CurrentMap != null &&
        Find.CurrentMap.gameConditionManager.ActiveConditions.Count > 0;
    }

    return allowed;
  }

  private static void InvokeWithContext<T>(MethodInfo method) where T : struct
  {
    method.Invoke(null, [GetContext<T>(method.DeclaringType)]);
  }

  private static Result RunTest(object instance, MethodInfo method, Benchmark.Measurement measurement)
  {
    Assert.IsTrue(method.GetParameters().Length == 0);
    Assert.IsTrue(method.ReturnType == typeof(void));
    return Benchmark.Run(instance, method.MethodHandle.GetFunctionPointer(), measurement);
  }

  private static Result RunTestWithReturn<R>(object instance, MethodInfo method, Benchmark.Measurement measurement)
  {
    Assert.IsTrue(method.GetParameters().Length == 0);
    Assert.IsTrue(method.ReturnType == typeof(R));
    return Benchmark.Run<R>(instance, method.MethodHandle.GetFunctionPointer(), measurement);
  }

  private static Result RunTestWithContext<T>(MethodInfo method,
    Benchmark.Measurement measurement) where T : struct
  {
    Assert.IsTrue(method.GetParameters().Length == 1);
    Assert.IsTrue(method.ReturnType == typeof(void));
    return Benchmark.Run(method.MethodHandle.GetFunctionPointer(), GetContext<T>(method.DeclaringType), measurement);
  }

  private static Result RunTestWithContextAndReturn<T, R>(MethodInfo method,
    Benchmark.Measurement measurement) where T : struct
  {
    Assert.IsTrue(method.GetParameters().Length == 1);
    Assert.IsTrue(method.ReturnType == typeof(R));
    return Benchmark.Run<T, R>(method.MethodHandle.GetFunctionPointer(), GetContext<T>(method.DeclaringType), measurement);
  }

  private static T GetContext<T>(Type declaringType) where T : struct
  {
    foreach (PropertyInfo propInfo in declaringType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
    {
      ContextAttribute contextAttr = propInfo.TryGetAttribute<ContextAttribute>();
      if (contextAttr is not null && propInfo.CanRead && propInfo.PropertyType == typeof(T))
      {
        return (T)propInfo.GetValue(null);
      }
    }
    foreach (FieldInfo fieldInfo in declaringType.GetFields(BindingFlags.Public | BindingFlags.Instance))
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
    private readonly Type type;
    public readonly string category;

    private readonly List<BenchmarkMethod> tests = [];
    private readonly List<BenchmarkMethod> setupMethods = [];
    private readonly List<BenchmarkMethod> onFinishMethods = [];
    private readonly bool runAsync;

    public BenchmarkMethods(string category, Type type, BenchmarkClassAttribute classAttr)
    {
      this.type = type;
      this.category = category;
      this.runAsync = classAttr.RunAsync;
      AllowedGameStates = classAttr.AllowedGameStates;

      Init();
    }

    public AllowedGameStates AllowedGameStates { get; }

    public MetaDataContainer MetaData { get; } = new();

    public bool HasTests => tests.Count > 0;

    private void Init()
    {
      foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
      {
        BenchmarkAttribute benchmarkAttr = method.TryGetAttribute<BenchmarkAttribute>();
        if (benchmarkAttr is not null)
        {
          if (!MethodIsSafe(method, out string reason))
          {
            Log.Error($"Unable to run {method.Name} benchmark. {reason}");
            continue;
          }
          tests.Add((benchmarkAttr.Label ?? method.Name, method));
        }
        else if (method.TryGetAttribute<PrepareAttribute>() is not null)
        {
          if (!MethodIsSafe(method, out string reason))
          {
            Log.Error($"Unable to run {method.Name} benchmark. {reason}");
            continue;
          }
          setupMethods.Add((method.Name, method));
        }
        else if (method.TryGetAttribute<OnFinishAttribute>() is not null)
        {
          if (!MethodIsSafe(method, out string reason))
          {
            Log.Error($"Unable to run {method.Name} benchmark. {reason}");
            continue;
          }
          onFinishMethods.Add((method.Name, method));
        }
      }
    }

    private static bool MethodIsSafe(MethodInfo method, out string reason)
    {
      reason = null;
      if (method.IsStatic || !method.IsPublic)
      {
        reason = "Method must be a public instance function.";
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

    public void Run()
    {
      LongEventHandler.QueueLongEvent(delegate
      {
        object instance = Activator.CreateInstance(type);
        InvokeMethods(instance, setupMethods);
        RunTests(instance);
        InvokeMethods(instance, onFinishMethods);
      }, string.Empty, runAsync, ExceptionHandler);
      return;

      static void ExceptionHandler(Exception ex)
      {
        Log.Error($"Exception thrown running benchmark.\n{ex}");
      }
    }

    private void RunTests(object instance)
    {
      List<(string, Result)> resultsByMethod = [];
      foreach ((string name, MethodInfo method) in tests)
      {
        LongEventHandler.SetCurrentEventText($"Running {name}");
        ParameterInfo[] parameters = method.GetParameters();
        Benchmark.Measurement measurement = MetaData.Get(MetaDataName.Measurement, 
          Benchmark.Measurement.Auto);
        switch (parameters.Length)
        {
          case 0:
          {
            Result results = method.ReturnType == typeof(void) ?
              RunTest(instance, method, measurement) :
              (Result)GenGeneric.InvokeStaticGenericMethod(typeof(BenchmarkManager),
                method.ReturnType, nameof(RunTestWithReturn), instance, method, measurement);
            resultsByMethod.Add((name, results));
          }
            break;
          case 1:
          {
            Result results = method.ReturnType == typeof(void) ?
              (Result)GenGeneric.InvokeStaticGenericMethod(typeof(BenchmarkManager),
                parameters[0].ParameterType, nameof(RunTestWithContext), instance, method, measurement) :
              (Result)AccessTools.Method(typeof(BenchmarkManager), nameof(RunTestWithContextAndReturn))
                .MakeGenericMethod(parameters[0].ParameterType, method.ReturnType)
                .Invoke(null, [instance, method, measurement]);

            resultsByMethod.Add((name, results));
          }
            break;
        }
      }
      OutputResults(this, resultsByMethod);
    }

    private static void InvokeMethods(object instance, List<BenchmarkMethod> methods)
    {
      foreach ((string name, MethodInfo method) in methods)
      {
        LongEventHandler.SetCurrentEventText($"Running {name}");
        ParameterInfo[] parameters = method.GetParameters();
        switch (parameters.Length)
        {
          case 0:
            method.Invoke(instance, []);
            break;
          case 1:
            GenGeneric.InvokeGenericMethod(instance, parameters[0].ParameterType, nameof(InvokeWithContext), method);
            break;
        }
      }
    }
  }
}