using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using JetBrains.Annotations;
using LudeonTK;
using RimWorld.Planet;
using Verse;
using BenchmarkMethod = System.ValueTuple<System.Type, string, System.Reflection.MethodInfo>;
using Result = DevTools.Benchmarking.Benchmark.Result;

namespace DevTools.Benchmarking;

internal class BenchmarkManager : IDevTool
{
  private const string ManagerName = "Benchmark";

  private readonly Dictionary<string, BenchmarkMethods> benchmarks = [];

  string IDevTool.ToolName => ManagerName;

  bool IDevTool.TryRegisterType(Type type)
  {
    BenchmarkClassAttribute attr = type.TryGetAttribute<BenchmarkClassAttribute>();
    if (attr is null)
      return false;
    string category = attr.Category ?? type.FullName;

    // Shouldn't be possible but ReSharper won't shut up so either we do a sanity check
    // or we disable the warning.
    if (category == null)
      throw new NullReferenceException(nameof(category));

    if (!benchmarks.ContainsKey(category))
      benchmarks[category] = new BenchmarkMethods(category, attr.RunAsync);
    benchmarks[category].AddFromType(type);
    return true;
  }

  void IDevTool.Init()
  {
  }

#region FrontEnd

  void IDevTool.OpenMenu()
  {
    List<DebugMenuOption> options = [];
    foreach (BenchmarkMethods methods in benchmarks.Values.OrderBy(bm => bm.category))
    {
      options.Add(new DebugMenuOption(methods.category, DebugMenuOptionMode.Action,
        () => RunBenchmarkFor(methods)));
    }
    Find.WindowStack.Add(new Dialog_DebugOptionListLister(options, ManagerName));
  }

  private static void OutputResults(string groupName,
    List<(string name, Result result)> resultsByMethod)
  {
    StringBuilder stringBuilder = new();

    stringBuilder.AppendLine($"----------     {groupName}     ----------");
    foreach ((string name, Result result) in resultsByMethod)
    {
      stringBuilder.AppendLine($"{name}: {result.TotalString}");
    }
    stringBuilder.Append("------------------------------------");

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
      allowed |= !WorldRendererUtility.WorldRenderedNow && Find.CurrentMap != null;

    if (allowedGameStates.HasFlag(AllowedGameStates.WorldRenderedNow))
      allowed |= WorldRendererUtility.WorldRenderedNow;

    if (allowedGameStates.HasFlag(AllowedGameStates.HasGameCondition))
      allowed |= !WorldRendererUtility.WorldRenderedNow && Find.CurrentMap != null &&
        Find.CurrentMap.gameConditionManager.ActiveConditions.Count > 0;

    return allowed;
  }

#endregion FrontEnd

  private static void RunBenchmarkFor(BenchmarkMethods benchmarks)
  {
    RunSetupFor(benchmarks);
    LongEventHandler.QueueLongEvent(() => RunBenchmarkMethods(benchmarks),
      string.Empty, benchmarks.runAsync, ExceptionHandler);
    return;

    static void ExceptionHandler(Exception ex)
    {
      Log.Error($"Exception thrown running benchmark.\n{ex}");
    }
  }

  private static void RunSetupFor(BenchmarkMethods benchmarks)
  {
    foreach ((Type type, MethodInfo method) in benchmarks.setupMethods)
    {
      ParameterInfo[] parameters = method.GetParameters();
      switch (parameters.Length)
      {
        case 0:
          method.Invoke(null, []);
          break;
        case 1:
          GenGeneric.InvokeStaticGenericMethod(
            typeof(BenchmarkManager),
            parameters[0].ParameterType, nameof(RunSetupWithContext), type, method);
          break;
      }
    }
  }

  private static void RunSetupWithContext<T>(Type type, MethodInfo method) where T : struct
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
      switch (parameters.Length)
      {
        case 0:
          resultsByMethod.Add((name, RunTest(method, benchmarks.sampleSize)));
          break;
        case 1:
          Result results = (Result)GenGeneric.InvokeStaticGenericMethod(
            typeof(BenchmarkManager),
            parameters[0].ParameterType, nameof(RunTestWithContext), type, method,
            benchmarks.sampleSize);
          resultsByMethod.Add((name, results));
          break;
      }
    }
    OutputResults(benchmarks.category, resultsByMethod);
  }

  private static unsafe Result RunTest(MethodInfo method, int sampleSize)
  {
    Assert.IsTrue(method.GetParameters().NullOrEmpty());
    delegate*<void> funcPtr = (delegate*<void>)method.MethodHandle.GetFunctionPointer();
    return Benchmark.Run(sampleSize, funcPtr);
  }

  private static unsafe Result RunTestWithContext<T>(Type declaringType, MethodInfo method,
    int sampleSize) where T : struct
  {
    // sanity check
    Assert.IsTrue(method.GetParameters().Length == 1);

    delegate*<ref T, void> funcPtr =
      (delegate*<ref T, void>)method.MethodHandle.GetFunctionPointer();
    return Benchmark.Run(sampleSize, funcPtr, GetContext<T>(declaringType));
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
    public int sampleSize = 1;
    public readonly List<BenchmarkMethod> tests = [];
    public readonly List<(Type parentType, MethodInfo method)> setupMethods = [];
    public readonly bool runAsync;

    public BenchmarkMethods(string category, bool runAsync)
    {
      this.category = category;
      this.runAsync = runAsync;
    }

    public void AddFromType(Type type)
    {
      Assert.IsTrue(type.TryGetAttribute<BenchmarkClassAttribute>().RunAsync == runAsync,
        "Mismatched RunAsync setting on benchmark categories.");
      SampleSizeAttribute sampleSizeAttr = type.TryGetAttribute<SampleSizeAttribute>();
      if (sampleSizeAttr is not null && sampleSizeAttr.Count > sampleSize)
        sampleSize = sampleSizeAttr.Count;

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
        else if (method.TryGetAttribute<SetUpAttribute>() is not null)
        {
          if (!MethodIsSafe(method, out string reason))
          {
            Log.Error($"Unable to run {method.Name} benchmark. {reason}");
            continue;
          }
          setupMethods.Add((type, method));
        }
      }
    }

    private static bool MethodIsSafe(MethodInfo method, out string reason)
    {
      reason = null;
      if (method.ReturnType != typeof(void))
      {
        reason = "Return type must be void.";
        return false;
      }
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
        if (pInfo.ParameterType.GetElementType() is null or { IsValueType: false } ||
          !pInfo.ParameterType.IsDefined(typeof(IsReadOnlyAttribute)))
        {
          reason = "Context type must be a readonly struct.";
          return false;
        }
      }
      return true;
    }
  }
}