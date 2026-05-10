using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace DevTools.Testing;

[DebuggerDisplay("Name = {Name}")]
internal class TestFixture : ITestFixture
{
  private readonly List<ITestFunction> setUps = [];
  private readonly List<ITestFunction> tearDowns = [];
  private readonly List<ITestFunction> oneTimeSetUps = [];
  private readonly List<ITestFunction> oneTimeTearDowns = [];
  private readonly List<ITestFunction> tests = [];

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