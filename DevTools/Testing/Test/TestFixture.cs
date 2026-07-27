using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using RimWorld;
using Verse;

namespace DevTools.Testing;

[DebuggerDisplay("Name = {Name}")]
internal class TestFixture : ITestFixture
{
  private readonly List<ITestFunction> setUps = [];
  private readonly List<ITestFunction> tearDowns = [];
  private readonly List<ITestFunction> oneTimeSetUps = [];
  private readonly List<ITestFunction> oneTimeTearDowns = [];
  private readonly List<ITestFunction> tests = [];
  private readonly List<Func<object, WorldGenerationSettings>> worldGen = [];
  private readonly List<Func<object, MapGenerationSettings>> mapGen = [];
  private readonly List<Func<object, Storyteller>>  storytellerGen = [];
  private readonly List<Func<object, Scenario>> scenarioGen = [];

  public TestFixture(ITestModule module, Type type, TestType testType)
  {
    Module = module;
    TestType = testType;
    Type = type;
    Name = Type.Name;
  }

  string ITestFixture.SaveFile => MetaData.Get<string>(MetaDataName.LoadSave);

  public IEnumerable<ITestFunction> TestFunctions => tests;

  public MetaDataContainer MetaData { get; } = new();

  public TestType TestType { get; }

  public Type Type { get; }

  public ITestModule Module { get; }

  public object[] Args { get; protected internal set; }

  object[] ITestCase.Args { get => Args; set => Args = value; }

  public virtual string Name { get; }

  public Status Status { get; set; } = Status.NotRun;

  WorldGenerationSettings ITestFixture.WorldGenerationSettings(object instance)
  {
    foreach (var func in worldGen)
    {
      var result = func(instance);
      if (result is not null)
        return result;
    }

    return null;
  }

  MapGenerationSettings ITestFixture.MapGenerationSettings(object instance)
  {
    foreach (var func in mapGen)
    {
      var result = func(instance);
      if (result is not null)
        return result;
    }
    
    return null;
  }

  public Scenario Scenario(object instance)
  {
    foreach (var func in scenarioGen)
    {
      var result = func(instance);
      if (result is not null)
        return result;
    }

    return null;
  }

  public Storyteller Storyteller(object instance)
  {
    foreach (var func in storytellerGen)
    {
      var result = func(instance);
      if (result is not null)
        return result;
    }

    return null;
  }

  bool ITestFixture.OneTimeSetUp(object instance)
  {
    return ExecuteAll(instance, oneTimeSetUps);
  }

  bool ITestFixture.OneTimeTearDown(object instance)
  {
    return ExecuteAll(instance, oneTimeTearDowns);
  }

  bool ITestFixture.SetUp(object instance)
  {
    return ExecuteAll(instance, setUps);
  }

  bool ITestFixture.TearDown(object instance)
  {
    return ExecuteAll(instance, tearDowns);
  }

  public virtual object CreateInstance()
  {
    return this.CreateTestClass();
  }

  private static bool ExecuteAll(object instance, List<ITestFunction> functions)
  {
    bool success = true;
    foreach (ITestFunction function in functions)
    {
      function.Status = Status.Pending;
      function.Execute(instance);
      success &= function.Status is Status.Pending;
    }
    return success;
  }

  public void AddFromType(Type type)
  {
    foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
      BindingFlags.Static | BindingFlags.Instance))
    {
      this.AddTestMethods<SetUpAttribute>(method, MethodType.SetUp, setUps);
      this.AddTestMethods<TearDownAttribute>(method, MethodType.TearDown, tearDowns);
      this.AddTestMethods<OneTimeSetUpAttribute>(method, MethodType.SetUp, oneTimeSetUps);
      this.AddTestMethods<OneTimeTearDownAttribute>(method, MethodType.TearDown, oneTimeTearDowns);
      this.AddTestMethods<TestAttribute>(method, MethodType.Test, tests);

      if (method.TryGetAttribute<WorldGenerationSettingsAttribute>() is not null)
        if (method.ReturnType != typeof(WorldGenerationSettings))
          Log.Error($"Unable to add {method.Name} to fixture. Return type must be WorldGenerationSettings");
        else
          worldGen.Add(obj =>
            method.Invoke(obj, null) as WorldGenerationSettings);

      if (method.TryGetAttribute<MapGenerationSettingsAttribute>() is not null)
        if (method.ReturnType != typeof(MapGenerationSettings))
          Log.Error($"Unable to add {method.Name} to fixture. Return type must be MapGenerationSettings");
        else
          mapGen.Add(obj =>
            method.Invoke(obj, null) as MapGenerationSettings);
      
      if (method.TryGetAttribute<StorytellerAttribute>() is not null)
        if (method.ReturnType != typeof(Storyteller))
          Log.Error($"Unable to add {method.Name} to fixture. Return type must be Storyteller");
        else
          storytellerGen.Add(obj =>
            method.Invoke(obj, null) as Storyteller);

      if (method.TryGetAttribute<ScenarioAttribute>() is not null)
        if (method.ReturnType != typeof(Scenario))
          Log.Error($"Unable to add {method.Name} to fixture. Return type must be Scenario");
        else
          scenarioGen.Add(obj =>
            method.Invoke(obj, null) as Scenario);
    }
  }

  public void SortByExecutionPriority()
  {
    setUps.Sort(TestCaseComparer.Default);
    tearDowns.Sort(TestCaseComparer.Default);
    oneTimeSetUps.Sort(TestCaseComparer.Default);
    oneTimeTearDowns.Sort(TestCaseComparer.Default);
    tests.Sort(TestCaseComparer.Default);
  }
}