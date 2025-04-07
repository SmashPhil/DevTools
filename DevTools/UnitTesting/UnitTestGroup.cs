using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Verse;

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
        Status status = method.Execute(out string failMessage);
        if (status < finalStatus)
          finalStatus = status;
        if (status == Status.Failed)
          FailMessage = failMessage;
        if (token.IsCancellationRequested)
          return;
      }

      // If Prepare fails don't run any further tests. They will likely fail anyways.
      if (finalStatus == Status.Failed)
        return;

      // Tests
      foreach (Method method in tests)
      {
        if (filter != null && filter.Any() && !filter.Contains(method))
          continue;

        Status status = method.Execute(out string failMessage);
        if (status < finalStatus)
          finalStatus = status;
        if (status == Status.Failed)
          FailMessage = failMessage;
        if (token.IsCancellationRequested)
          return;
      }

      // Clean Up
      foreach (Method method in cleanUps)
      {
        Status status = method.Execute(out string failMessage);
        if (status < finalStatus)
          finalStatus = status;
        if (status == Status.Failed)
          FailMessage = failMessage;
        if (token.IsCancellationRequested)
          return;
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

  public class Method
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

    public MethodType Type { get; }

    public Status Result { get; private set; }

    public List<(string name, bool result)> ResultList { get; } = [];

    public string Name => method.Name;

    public Status Execute(out string failMessage)
    {
      failMessage = null;
      try
      {
        ResultList.Clear();
        using (new Expect.TestWatcher(ResultList))
        {
          method.Invoke(instance, emptyArgs);
        }
        Result = ResultList.All(tuple => tuple.result) ? Status.Passed : Status.Failed;
      }
      catch (AssertFailException ex)
      {
        failMessage = $"[{Type}::{method.Name}] Assertion failed!\n{ex}";
        return Status.Failed;
      }
      catch (Exception ex)
      {
        failMessage = $"[{Type}::{method.Name}] Exception thrown!\n{ex}";
        return Status.Failed;
      }
      return Status.Passed;
    }

    internal enum MethodType
    {
      Prepare,
      Test,
      CleanUp,
    }
  }
}