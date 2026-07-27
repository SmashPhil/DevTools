using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine.Assertions;
using Verse;

namespace DevTools.Testing;

[PublicAPI]
public static class TestExtensions
{
  public static bool HasParameterSource(ParameterInfo pinfo)
  {
    return pinfo.GetCustomAttribute<ParametersSourceAttribute>() != null ||
           pinfo.GetCustomAttribute<ParametersAttribute>() != null ||
           pinfo.GetCustomAttribute<DefParameterAttribute>() != null;
  }

  public static object CreateTestClass(this ITestFixture fixture)
  {
    return fixture.Args.Length == 0 ?
      Activator.CreateInstance(fixture.Type) :
      Activator.CreateInstance(fixture.Type, fixture.Args);
  }

  public static bool IsSubRoutine(this ITestFunction function)
  {
    return function.MethodInfo.ReturnType == typeof(IEnumerator);
  }

  public static void AddTestMethods<T>(this ITestFixture fixture, MethodInfo methodInfo, MethodType methodType,
    List<ITestFunction> methodList) where T : Attribute
  {
    if (methodInfo.TryGetAttribute<T>() is null)
      return;

    if (!MethodIsSafe(methodInfo, out string reason))
    {
      Log.Error($"Unable to add {methodInfo.Name} to fixture. {reason}");
      return;
    }

    if (methodInfo.MissingRequiredMods())
      return;

    foreach (ITestFunction function in CreateAllTests(CreateFunction, methodInfo))
    {
      methodList.Add(function);
    }
    return;

    ITestFunction CreateFunction() => new TestFunction(fixture, methodInfo, methodType);

    static bool MethodIsSafe(MethodInfo method, out string reason)
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
      }
      return true;
    }
  }

  public static bool MissingRequiredMods(this ITestCase testCase)
  {
    if (testCase.MetaData.Get<string[]>(MetaDataName.LoadIfAllModsActive) is { } packageIdsAll &&
        !ModLister.AllModsActiveNoSuffix(packageIdsAll))
    {
      return true;
    }
    return testCase.MetaData.Get<string[]>(MetaDataName.LoadIfAnyModsActive) is { } packageIdsAny &&
           !ModLister.AnyModActiveNoSuffix(packageIdsAny);
  }

  public static bool MissingRequiredMods(this MemberInfo memberInfo)
  {
    if (memberInfo.TryGetAttribute<LoadIfModsActiveAttribute>() is { } loadIfModsActive &&
        !ModLister.AllModsActiveNoSuffix(loadIfModsActive.PackageIds))
    {
      return true;
    }
    return memberInfo.TryGetAttribute<LoadIfAnyModsActiveAttribute>() is { } loadIfAnyModActive &&
           !ModLister.AnyModActiveNoSuffix(loadIfAnyModActive.PackageIds);
  }

  public static List<object[]> ExtractParameters(MethodBase method)
  {
    ParameterInfo[] parameters = method.GetParameters();
    if (parameters.Length == 0)
      return [];

    if (parameters.Length > 0 && !parameters.All(HasParameterSource))
    {
      Log.Error("Tests do not support parameters with no source attribute");
      return [];
    }

    if (parameters.Length > 1)
    {
      Log.Error("Multiple parameters with source attributes is not yet supported.");
      return [];
    }

    Type type = method.DeclaringType;
    Assert.IsNotNull(type);
    ParameterInfo pinfo = parameters[0];
    List<object[]> args = [];
    if (pinfo.GetCustomAttribute<ParametersSourceAttribute>() is { } sourceAttr)
    {
      Type fieldType = sourceAttr.Type ?? type!;
      FieldInfo sourceField = AccessTools.Field(fieldType, sourceAttr.FieldName);
      if (sourceField is null)
      {
        Log.Error($"Unable to find parameter source {fieldType}.{sourceAttr.FieldName}.");
        return [];
      }
      Array paramArray = (Array)sourceField.GetValue(null);
      foreach (object obj in paramArray)
      {
        args.Add([obj]);
      }
    }
    else if (pinfo.GetCustomAttribute<ParametersAttribute>() is { } paramsAttr)
    {
      foreach (object obj in paramsAttr.Arguments)
      {
        args.Add([obj]);
      }
    }
    else if (pinfo.GetCustomAttribute<DefParameterAttribute>() is {} defParamAttr)
    {
      HashSet<string> allowedPackageIds = defParamAttr.OnlyFromMods?.ToHashSet();
      var defs = GenDefDatabase.GetAllDefsInDatabaseForDef(pinfo.ParameterType);
      foreach (Def def in defs)
      {
        // Defs with no mod assigned are runtime defs that were incorrectly added, or are mock defs.
        if (def.modContentPack is null)
          continue;

        if (allowedPackageIds == null ||
            allowedPackageIds.Contains(def.modContentPack.PackageIdPlayerFacing.ToLowerInvariant()))
        {
          args.Add([def]);
        }
      }
    }
    else
    {
      Log.Error($"Unable to register test fixture {type.Name}. MethodBase has parameters with no input attribute.");
    }
    return args;
  }

  public static IEnumerable<T> CreateAllFixtures<T>(Func<T> factory, MemberInfo memberInfo, ConstructorInfo ctor)
    where T : ITestFixture
  {
    List<object[]> args = ExtractParameters(ctor);
    if (args.Count == 0)
    {
      T fixture = factory();
      fixture.MetaData.Load(memberInfo);
      if (!fixture.MetaData.Get<bool>(MetaDataName.Disabled))
      {
        fixture.Args = [];
        yield return fixture;
      }
      yield break;
    }

    foreach (object[] arg in args)
    {
      T fixture = factory();
      fixture.MetaData.Load(memberInfo);
      if (fixture.MetaData.Get<bool>(MetaDataName.Disabled))
        continue;

      fixture.Args = [.. arg];
      yield return fixture;
    }
  }

  public static IEnumerable<T> CreateAllTests<T>(Func<T> factory, MethodInfo methodInfo)
    where T : ITestFunction
  {
    if (SetUpTestCase(factory, methodInfo, out List<T> functions))
    {
      foreach (T function in functions)
      {
        if (!function.MetaData.Get<bool>(MetaDataName.Disabled))
        {
          yield return function;
        }
      }
      yield break;
    }
    List<object[]> args = ExtractParameters(methodInfo);
    if (args.Count == 0)
    {
      T function = factory();
      function.MetaData.Load(methodInfo);
      if (!function.MetaData.Get<bool>(MetaDataName.Disabled))
      {
        function.Args = [];
        yield return function;
      }
      yield break;
    }
    foreach (object[] arg in args)
    {
      T function = factory();
      function.MetaData.Load(methodInfo);
      if (function.MetaData.Get<bool>(MetaDataName.Disabled))
        continue;

      function.Args = arg;
      yield return function;
    }
    yield break;

    static bool SetUpTestCase(Func<T> factory, MethodInfo methodInfo, out List<T> functions)
    {
      var attributes = methodInfo.GetCustomAttributes<TestCaseAttribute>().ToList();
      if (attributes.Count > 0)
      {
        functions = [];
        foreach (TestCaseAttribute attr in attributes)
        {
          if (attr.Arguments == null)
          {
            Log.Error($"Missing arguments for test case {methodInfo.Name}");
            continue;
          }
          T function = factory();
          function.MetaData.Load(methodInfo);
          function.Args = [.. attr.Arguments];
          function.ExpectedResult = attr.ExpectedResult;
          functions.Add(function);
        }
        return true;
      }
      functions = null;
      return false;
    }
  }
}