using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using DevTools.Benchmarking;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;

namespace DevTools.UnitTesting;

internal class UnitTestGroup : ITestCase, IDataRow<ExplorerColumn>
{
  private readonly List<Method> setUps = [];
  private readonly List<Method> tests = [];
  private readonly List<Method> tearDowns = [];

  private readonly Stopwatch groupTimer = new();

  private string failMessageInt;

  private readonly Dictionary<Type, object> instanceByType = [];

  public UnitTestGroup(string alias, TestType type)
  {
    Alias = alias;
    Type = type;
  }

  public string Alias { get; }

  public MetaDataContainer MetaData { get; } = new();

  public TestType Type { get; }

  public Benchmark.Result Duration { get; private set; }

  public Status Status { get; private set; } = Status.NotRun;

  public string FailLabel { get; private set; }

  public string FailMessage
  {
    get { return failMessageInt; }
    private set
    {
      failMessageInt = value;
      if (FailMessage == null)
      {
        FailLabel = null;
      }
      else
      {
        int newlineIdx =
          FailMessage.IndexOf(Environment.NewLine, StringComparison.InvariantCulture);
        FailLabel = newlineIdx < 0 ?
          FailMessage :
          FailMessage.Substring(0, newlineIdx);
      }
    }
  }

  public int TestCount => tests.Count;

  string ITestCase.Name => TestCount > 1 ? $"{Alias} ({TestCount})" : Alias;

  float IDataRow<ExplorerColumn>.Height => ExplorerColumn.LineHeight;

  bool IDataRow<ExplorerColumn>.CanExpand =>
    TestCount > 1 || (TestCount == 1 && tests[0].Root?.Groups.Count > 1);

  bool IDataRow<ExplorerColumn>.Expanded { get; set; }

  IEnumerable<IDataRow<ExplorerColumn>> IDataRow<ExplorerColumn>.NestedRows
  {
    get
    {
      foreach (Method method in tests)
        yield return method;
    }
  }

  private static bool ExecutingOn(TestType type)
  {
    return type switch
    {
      TestType.MainMenu     => Current.ProgramState == ProgramState.Entry,
      TestType.Playing      => Current.ProgramState == ProgramState.Playing,
      TestType.PostGameExit => Current.ProgramState == ProgramState.Entry,
      TestType.Disabled     => false,
      _                     => throw new NotImplementedException(nameof(TestType))
    };
  }

  public void Execute(CancellationToken token, HashSet<Method> filter = null)
  {
    FailMessage = null;
    Assert.IsTrue(ExecutingOn(Type),
      $"Executing unit test {Alias} on wrong TestType {Type}.");

    if (TestCount == 0)
    {
      Status = Status.Skipped;
      return;
    }

    using StackTraceCacheDisabler stcd = new();

    Status = Status.Pending;
    groupTimer.Restart();
    Status finalStatus = Status.Passed;
    try
    {
      if (token.IsCancellationRequested)
        return;

      Test.Log($"----------  Running {Alias}");
      // Set Up
      foreach (Method method in setUps)
      {
        method.Execute(out string failMessage);

        if (method.Status < finalStatus)
          finalStatus = method.Status;
        if (method.Status == Status.Failed)
          FailMessage = failMessage;
        if (token.IsCancellationRequested)
        {
          // We need to perform cleanup before we can cancel testing
          finalStatus = Status.Canceled;
          break;
        }
      }

      // If SetUp has any other status except passed, skip testing. There is a good chance
      // the tests will be invalid.
      if (finalStatus == Status.Passed)
      {
        // Tests
        foreach (Method method in tests)
        {
          if (filter != null && filter.Any() && !filter.Contains(method))
            continue;

          method.Execute(out string failMessage);

          if (method.Status < finalStatus)
            finalStatus = method.Status;
          if (method.Status == Status.Failed)
            FailMessage = failMessage;
          if (token.IsCancellationRequested)
            return;
        }
      }

      // Tear Down
      foreach (Method method in tearDowns)
      {
        method.Execute(out string failMessage);

        if (method.Status < finalStatus)
          finalStatus = method.Status;
        if (method.Status == Status.Failed)
          FailMessage = failMessage;
      }
    }
    finally
    {
      groupTimer.Stop();
      Duration = new Benchmark.Result(groupTimer, 1, Benchmark.Measurement.Milliseconds);
      Status = token.IsCancellationRequested ? Status.Canceled : finalStatus;
    }
  }

  public void AddFromType(Type type)
  {
    foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
      BindingFlags.Static | BindingFlags.Instance))
    {
      TryAddMethod<SetUpAttribute>(type, method, Method.MethodType.SetUp, setUps);
      TryAddMethod<TestAttribute>(type, method, Method.MethodType.Test, tests);
      TryAddMethod<TearDownAttribute>(type, method, Method.MethodType.TearDown, tearDowns);
    }
    return;

    void TryAddMethod<T>(Type declaringType, MethodInfo methodInfo,
      Method.MethodType methodType,
      List<Method> methodList) where T : Attribute
    {
      if (methodInfo.TryGetAttribute<T>() is not null)
      {
        if (!MethodIsSafe(methodInfo, out string reason))
        {
          Log.Error($"Unable to add {methodInfo.Name} to unit test. {reason}");
          return;
        }
        object instance = null;
        // Static types are both abstract and sealed
        if (declaringType.IsAbstract)
        {
          // Only static types should be getting added as a unit test method if the type
          // is abstract, otherwise we wouldn't be able to invoke the method.
          Assert.IsTrue(declaringType.IsSealed);
        }
        else
        {
          if (!instanceByType.TryGetValue(declaringType, out instance))
          {
            instance = Activator.CreateInstance(declaringType);
            instanceByType[declaringType] = instance;
          }
        }
        Method method = new(instance, methodInfo, methodType);
        method.MetaData.Load(methodInfo);
        methodList.Add(method);
      }
    }
  }

  public void SortByExecutionPriority()
  {
    setUps.Sort();
    tests.Sort();
    tearDowns.Sort();
  }

  private static bool MethodIsSafe(MethodInfo method, out string reason)
  {
    reason = null;
    if (method.ReturnType != typeof(void))
    {
      reason = "Return type must be void.";
      return false;
    }
    ParameterInfo[] parameters = method.GetParameters();
    if (parameters.Length > 0)
    {
      reason = "Parameter count doesn't match any designated benchmark method.";
      return false;
    }
    return true;
  }

  public void TestOutcomes(StatusCount statusCount)
  {
    foreach (Method method in setUps)
      method.TestOutcomes(statusCount);
    foreach (Method method in tests)
      method.TestOutcomes(statusCount);
    foreach (Method method in tearDowns)
      method.TestOutcomes(statusCount);
  }

  void IDataRow<ExplorerColumn>.Draw(Rect rect, ExplorerColumn column)
  {
    column.Draw(rect, this);
  }

  public class Method : IComparable<Method>, ITestCase, IDataRow<ExplorerColumn>
  {
    private static readonly object[] emptyArgs = [];

    private readonly object instance;
    private readonly MethodInfo method;

    public Method(object instance, MethodInfo method, MethodType methodType)
    {
      this.instance = instance;
      this.method = method;
      Type = methodType;
    }

    public MethodType Type { get; }

    public MetaDataContainer MetaData { get; } = new();

    public ContextGroup Root { get; private set; } = new(null);

    public Dictionary<string, string> Traits => Root.Traits;

    public Status Status => Root.Status;

    public string FailLabel => Root.FailLabel;
    public string FailMessage => Root.FailMessage;

    public Benchmark.Result Duration => Root.Duration;

    public string Name => method.Name;

    public int TestCount => Root.TestCount;

    bool IDataRow<ExplorerColumn>.CanExpand => Root.CanExpand;

    bool IDataRow<ExplorerColumn>.Expanded { get; set; }

    float IDataRow<ExplorerColumn>.Height => ExplorerColumn.LineHeight;

    IEnumerable<IDataRow<ExplorerColumn>> IDataRow<ExplorerColumn>.NestedRows
    {
      get
      {
        if (Root.Groups.Count > 1)
        {
          foreach (ContextGroup group in Root.Groups)
            yield return group;
        }
      }
    }

    public void Execute(out string failMessage)
    {
      Test.Log($"Executing {Type}::{Name}");
      Root.Reset();
      failMessage = null;

      // Empty group to capture test results at the root level
      using Test.Group group = new(null);
      Root = Test.CurrentGroup;
      Root.TestCase = this;
      try
      {
        method.Invoke(instance, emptyArgs);
        ContextGroup.TabulateTestResultsRecursive(Root);
      }
      catch (TargetInvocationException ex) when (ex.InnerException is AssertionException)
      {
        Test.Log(ex.InnerException.ToString());
        Test.CurrentGroup.FailMessage = ex.InnerException.Message;
        Test.CurrentGroup.StackTrace = ex.InnerException.StackTrace;
        Test.CurrentGroup.Exception = ex.InnerException;
        Test.CurrentGroup.Status = Status.Failed;
      }
      catch (Exception ex)
      {
        Test.Log(ex.ToString());
        Test.CurrentGroup.FailMessage =
          $"{ex.InnerException?.GetType().Name ?? ex.GetType().Name} thrown.";
        Test.CurrentGroup.StackTrace = ex.InnerException?.StackTrace ?? ex.StackTrace;
        Test.CurrentGroup.Exception = ex;
        Test.CurrentGroup.Status = Status.Failed;
      }
      finally
      {
        failMessage = Test.CurrentGroup.FailMessage;
      }
    }

    int IComparable<Method>.CompareTo(Method other)
    {
      // There should never be any null Method entries. UnitTestManager was not initialized
      // properly and testing may throw as well.
      if (other is null)
        throw new NullReferenceException();

      int lhsInt = MetaData.Get<int>(MetaDataName.ExecutionPriority);
      int rhsInt = other.MetaData.Get<int>(MetaDataName.ExecutionPriority);

      // Higher priority => earlier in the list
      if (lhsInt == rhsInt)
        return 0;
      if (lhsInt > rhsInt)
        return 1;
      return -1;
    }

    public void TestOutcomes(StatusCount statusCount)
    {
      Root?.TestOutcomes(statusCount);
    }

    void IDataRow<ExplorerColumn>.Draw(Rect rect, ExplorerColumn column)
    {
      column.Draw(rect, this);
    }

    internal enum MethodType
    {
      SetUp,
      Test,
      TearDown,
    }
  }
}