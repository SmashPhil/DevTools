using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Verse;
using NullReferenceException = System.NullReferenceException;

namespace DevTools.UnitTesting;

internal class UnitTestGroup
{
  private readonly List<Method> prepares = [];
  private readonly List<Method> cleanUps = [];

  internal readonly List<Method> tests = [];

  public UnitTestGroup(string category, TestType type, bool runAsync)
  {
    Category = category;
    Type = type;
    RunAsync = runAsync;
  }

  public string Category { get; }

  public TestType Type { get; }

  public bool RunAsync { get; }

  public Status Result { get; private set; }

  public string FailMessage { get; private set; }

  private static bool ExecutingOn(TestType type)
  {
    return type switch
    {
      TestType.MainMenu => Current.ProgramState == ProgramState.Entry,
      TestType.Playing  => Current.ProgramState == ProgramState.Playing,
      _                 => false,
    };
  }

  public void Execute(CancellationToken token, HashSet<Method> filter = null)
  {
    FailMessage = null;
    Assert.IsTrue(ExecutingOn(Type),
      $"Executing unit test {Category} on wrong TestType.");

    using Assert.ThrowOnAssertEnabler te = new();

    Result = Status.Pending;

    Status finalStatus = Status.Passed;
    try
    {
      if (token.IsCancellationRequested)
        return;

      // Prepare
      foreach (Method method in prepares)
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

      // If Prepare has any other status except passed, skip testing. There is a good chance
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

      // Clean Up
      foreach (Method method in cleanUps)
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
      Result = token.IsCancellationRequested ? Status.Canceled : finalStatus;
    }
  }

  public void AddFromType(Type type)
  {
    foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
      BindingFlags.Static | BindingFlags.Instance))
    {
      TryAddMethod<PrepareAttribute>(type, method, Method.MethodType.Prepare, prepares);
      TryAddMethod<TestAttribute>(type, method, Method.MethodType.Test, tests);
      TryAddMethod<CleanUpAttribute>(type, method, Method.MethodType.CleanUp, cleanUps);
    }
    return;

    static void TryAddMethod<T>(Type declaringType, MethodInfo methodInfo,
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
        methodList.Add(new Method(declaringType, methodInfo, methodType));
      }
    }
  }

  public void SortByExecutionPriority()
  {
    prepares.Sort();
    tests.Sort();
    cleanUps.Sort();
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

  public class Method : IComparable<Method>
  {
    private static readonly object[] emptyArgs = [];

    private readonly object instance;
    private readonly MethodInfo method;

    public Method(Type declaringType, MethodInfo method, MethodType methodType)
    {
      this.method = method;
      Type = methodType;

      // Static type
      if (declaringType.IsAbstract)
      {
        // Only static types should be getting added as a unit test method if the type
        // is abstract, otherwise we wouldn't be able to invoke the method.
        Assert.IsTrue(declaringType.IsSealed);
        return;
      }
      instance = Activator.CreateInstance(declaringType);
    }

    private MethodType Type { get; }

    public Status Status { get; private set; }

    public string ExtraInfo { get; private set; }

    private List<(string name, Status status)> ResultList { get; } = [];

    public string Name => method.Name;

    public void Execute(out string failMessage)
    {
      failMessage = null;
      try
      {
        ResultList.Clear();
        using (new Expect.TestWatcher(ResultList))
        {
          method.Invoke(instance, emptyArgs);
        }
        Status = Status.Passed;
        foreach ((string name, Status status) in ResultList)
        {
          if (status is Status.Failed)
          {
            Status = status;
            break;
          }
          if (status is Status.Canceled or Status.Skipped)
          {
            // Reason for skip or cancellation will be in the attached label
            ExtraInfo = name;
            Status = status;
            break;
          }
        }
      }
      catch (AssertFailException ex)
      {
        failMessage = $"[{Type}::{method.Name}] Assertion failed!\n{ex}";
        Status = Status.Failed;
      }
      catch (Exception ex)
      {
        failMessage = $"[{Type}::{method.Name}] Exception thrown!\n{ex}";
        Status = Status.Failed;
      }
    }

    int IComparable<Method>.CompareTo(Method other)
    {
      // There should never be any null Method entries. UnitTestManager was not initialized
      // properly and testing may throw as well.
      if (other is null)
        throw new NullReferenceException();

      ExecutionPriorityAttribute lhsAttr = method.TryGetAttribute<ExecutionPriorityAttribute>();
      ExecutionPriorityAttribute rhsAttr = other.method.TryGetAttribute<ExecutionPriorityAttribute>();
      int lhsInt = lhsAttr?.Priority ?? 0;
      int rhsInt = rhsAttr?.Priority ?? 0;

      if (lhsInt == rhsInt)
        return 0;
      if (lhsInt < rhsInt)
        return -1;
      return 1;
    }

    internal enum MethodType
    {
      Prepare,
      Test,
      CleanUp,
    }
  }
}