using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Threading;
using DevTools.Benchmarking;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;

namespace DevTools.Testing;

[PublicAPI]
public static class Test
{
  private static readonly Dictionary<ITestManager, TestCache> cache = new();
  private static readonly StringBuilder statusBuilder = new();

  public static ITestGroup Current => ActiveSession?.CurrentGroup;

  private static TestCache CurrentCache => cache.TryGetValue(ActiveSession.TestManager);

  private static Session ActiveSession { get; set; }

  public static IEnumerable<ITestGroup> GetGroups(ITestManager manager)
  {
    return cache.TryGetValue(manager)?.AssemblyGroups;
  }

  private static ITestGroup BeginGroup([NotNull] ITestFixture fixture)
  {
    TestData testGroup = CurrentCache.GetOrAdd(fixture);
    ActiveSession.Push(testGroup);
    return testGroup;
  }

  private static ITestGroup BeginGroup(ITestFunction function)
  {
    TestData testGroup = CurrentCache.GetOrAdd(function);
    ActiveSession.Push(testGroup);
    return testGroup;
  }

  private static void EndGroup()
  {
    ActiveSession.Pop();
  }

  public static ITestGroup GetEntry([NotNull] ITestCase testCase)
  {
    return CurrentCache.Get(testCase);
  }

  public static void ResetAll(ITestManager manager)
  {
    cache.TryGetValue(manager)?.ResetStatuses();
  }

  public static void Cancel(string message = null)
  {
    Expect.SendSignal(Status.Canceled, "Test.Cancel", message, skipFrames: 2);
  }

  public static void Skip(string message = null)
  {
    Expect.SendSignal(Status.Skipped, "Test.Skip", message, skipFrames: 2);
  }

  public static void Fail(string message, string failureMessage = null)
  {
    Expect.SendSignal(Status.Failed, "Test.Fail", message, failureMessage);
  }

  public static void Fail(Exception ex)
  {
    ITestGroup current = Current;
    current.Exception ??= ex.InnerException ?? ex;
    Expect.SendSignal(Status.Failed,
      context: "Exception thrown!",
      message: null);
  }

  public static IEnumerator Suspend(float secondsTimeOut, string message = null)
  {
    int countdownTime = Mathf.CeilToInt(secondsTimeOut);
    using CancellationTokenSource token = new(countdownTime < 0 ? int.MaxValue : countdownTime * 1000);

    Dialog_TestSuspension dlg = new(message, countdownTime, token);
    Find.WindowStack.Add(dlg);
    while (!token.IsCancellationRequested)
      yield return null;
    Find.WindowStack.TryRemove(dlg);
  }

  internal static void Discover(ITestManager manager)
  {
    // Do a dry run through all cached test cases, test data is cached lazily so entering scopes
    // will create an entry if it hasn't already.
    using (new SessionScope(manager))
    {
      foreach (ITestFixture fixture in manager.TestFixtures)
      {
        using Scope fxs = new(fixture);
        foreach (ITestFunction function in fixture.TestFunctions)
        {
          if (function.MethodType is MethodType.Test)
          {
            using Scope fns = new(function);
          }
        }
      }
    }
    ResetAll(manager);
  }

  internal static void LogResults(ITestFixture fixture, List<ITestFunction> functions)
  {
    ITestGroup fixtureGroup = GetEntry(fixture);
    Log(fixtureGroup, StatusMessage(fixtureGroup));
    foreach (ITestFunction function in functions)
    {
      ITestGroup functionGroup = GetEntry(function);
      Log(functionGroup, "--\t" + StatusMessage(functionGroup));
    }
    Log(fixtureGroup, string.Empty); // newline if fixture logged
    return;

    static void Log(ITestGroup group, string message)
    {
      if (group.Status is Status.Failed)
      {
        DevLog.Write(message);
        string stackTrace = group.Exception?.StackTrace ?? group.StackTrace?.ToString();
        if (!stackTrace.NullOrEmpty())
        {
          DevLog.Write(stackTrace);
        }
      }
      else
      {
        DevLog.WriteVerbose(message);
      }
    }
  }

  internal static string StatusMessage(this ITestGroup group)
  {
    const string FailedLabel = "[Failed]";
    const string CanceledLabel = "[Canceled]";
    const string SkippedLabel = "[Skipped]";
    const string PassedLabel = "[Passed]";
    const string PendingLabel = "[Pending]";
    const string NotRunLabel = "[NotRun]";

    statusBuilder.Clear();
    statusBuilder.Append(group.Status switch
    {
      Status.Failed => $"{FailedLabel}   ",
      Status.Canceled => $"{CanceledLabel} ",
      Status.Skipped => $"{SkippedLabel}  ",
      Status.Passed => $"{PassedLabel}   ",
      Status.Pending => $"{PendingLabel}  ",
      Status.NotRun => $"{NotRunLabel}   ",
      _ => throw new NotImplementedException(nameof(Status)),
    });
    statusBuilder.Append($" {group.Label}");
    if (group.Status is Status.Failed)
    {
      if (!group.TestContext.NullOrEmpty())
      {
        statusBuilder.Append($"  {group.TestContext}");
      }
      if (!group.FailLabel.NullOrEmpty())
      {
        statusBuilder.Append($"  {group.FailLabel}");
      }
      if (!group.FailMessage.NullOrEmpty())
      {
        statusBuilder.Append($"  {group.FailMessage}");
      }
      if (group.Exception != null)
      {
        statusBuilder.AppendLine();
        statusBuilder.AppendLine(group.Exception.ToString());
      }
    }
    statusBuilder.AppendLine();
    return statusBuilder.ToString().TrimEnd();
  }

  public readonly struct Scope : IDisposable
  {
    public Scope(ITestFixture fixture)
    {
      BeginGroup(fixture);
    }

    public Scope(ITestFunction function)
    {
      BeginGroup(function);
    }

    public void Dispose()
    {
      EndGroup();
    }
  }

  internal readonly struct SessionScope : IDisposable
  {
    public SessionScope(ITestManager manager)
    {
      if (ActiveSession != null)
      {
        Log.Error("Trying to start session when one is still active.");
        return;
      }
      ActiveSession = new Session(manager);
    }

    public void Dispose()
    {
      ActiveSession = null;
    }
  }

  private sealed class Session
  {
    private readonly Stack<TestData> dataStack = [];

    public ITestManager TestManager { get; }

    public TestData CurrentGroup { get; private set; }

    public Session(ITestManager manager)
    {
      TestManager = manager;
      if (!cache.ContainsKey(manager))
      {
        cache[manager] = new TestCache();
      }
    }

    internal void Push(TestData entry)
    {
      if (CurrentGroup != null)
      {
        dataStack.Push(CurrentGroup);
      }
      CurrentGroup = entry;
      CurrentGroup.Start();
    }

    internal void Pop()
    {
      CurrentGroup.End();
      // Use interface property for restricted setter where status only elevates, never overwrites.
      ((ITestGroup)CurrentGroup).Status = Status.Passed;
      CurrentGroup = dataStack.Count > 0 ? dataStack.Pop() : null;
    }
  }

  private sealed class TestCache
  {
    private readonly Dictionary<Assembly, TestData> assemblyData = [];
    private readonly Dictionary<string, TestData> testGroups = [];
    private readonly Dictionary<Assembly, Dictionary<ITestCase, TestData>> tests = [];

    public IEnumerable<ITestGroup> AssemblyGroups => assemblyData.Values;

    private TestData GetModuleGroup(ITestCase testCase)
    {
      var module = testCase.Type.Assembly;
      if (!tests.TryGetValue(module, out var moduleCache))
      {
        var moduleGroup = new TestData(module.GetName().Name);
        moduleCache = [];
        tests[module] = moduleCache;
        assemblyData[module] = moduleGroup;
      }
      return assemblyData.TryGetValue(module);
    }

    public TestData GetOrAdd(ITestCase testCase)
    {
      // NOTE: GetModuleGroup ensures test dict is cached, could be refactored for scope.
      TestData moduleGroup = GetModuleGroup(testCase);
      var assemblyTests = tests[testCase.Type.Assembly];
      if (!assemblyTests.TryGetValue(testCase, out TestData testGroup))
      {
        testGroup = new TestData(testCase);
        assemblyTests[testCase] = testGroup;
        TestData currentGroup = ActiveSession.CurrentGroup ?? moduleGroup;
        Assert.IsNotNull(currentGroup);
        if (testCase.Args.Length > 0)
        {
          if (!testGroups.TryGetValue(testCase.Name, out TestData fixtureGroup))
          {
            fixtureGroup = new TestData(testCase.Name);
            testGroups[testCase.Name] = fixtureGroup;
            currentGroup.AddChild(fixtureGroup);
          }
          fixtureGroup.AddChild(testGroup);
        }
        else
        {
          currentGroup.AddChild(testGroup);
        }
      }
      return testGroup;
    }

    public TestData Get(ITestCase testCase)
    {
      var module = testCase.Type.Assembly;
      if (!tests.TryGetValue(module, out var moduleCache))
      {
        return null;
      }
      return moduleCache.TryGetValue(testCase);
    }

    public void ResetStatuses()
    {
      foreach (TestData group in assemblyData.Values)
      {
        group.Reset();
      }
    }
  }

  [DebuggerDisplay("Label = {Label}")]
  private sealed class TestData : ITestGroup
  {
    private readonly Stopwatch stopwatch = new();
    private readonly List<TestData> children = [];

    public TestData(string name)
    {
      Label = name;
    }

    /// <summary>
    /// Initializes a new instance of the TestData class.
    /// </summary>
    /// <param name="testCase">The test case associated with this data. Cannot be null.</param>
    public TestData([NotNull] ITestCase testCase)
    {
      Label = testCase.Name;
      TestCase = testCase;
      Tooltip = TestCase.MetaData.Get<string>(MetaDataName.Description);
      ShouldHide = TestCase.MetaData.Get<bool>(MetaDataName.Disabled);
    }

    public ITestCase TestCase { get; }

    private ITestGroup Parent { get; set; }

    public IEnumerable<ITestGroup> Children => children;

    public string Label
    {
      get
      {
        if (TestCase is null || TestCase.Args.Length == 0)
          return field;

        return $"{field}({string.Join(", ", TestCase.Args)})";
      }
    }

    public string Tooltip { get; }

    public bool ShouldHide { get; }

    public bool CanExpand => children.Count > 0 || children.Exists(static child => child.CanExpand);

    bool IDataRow<ExplorerColumn>.Expanded { get; set; }

    float IDataRow<ExplorerColumn>.Height => ExplorerColumn.LineHeight;

    IEnumerable<IDataRow<ExplorerColumn>> IDataRow<ExplorerColumn>.NestedRows => children;

    public Benchmark.Result Duration { get; set; }

    private Status Status
    {
      get
      {
        return TestCase?.Status ?? field;
      }
      set
      {
        // Interface setter implementation only accepts status 'elevations' from test runner,
        // allowing test failures to persist for a parent.
        field = value;
        TestCase?.Status = field;
        Parent?.Status = field;
      }
    } = Status.NotRun;

    Status ITestGroup.Status
    {
      get => Status;
      set
      {
        if (value > Status)
          return;

        Status = value;
      }
    }

    public string TestContext { get; set; }

    public string FailLabel
    {
      get;
      set
      {
        field = value;
        Parent?.FailLabel = field;
      }
    }

    public string FailMessage
    {
      get;
      set
      {
        field = value;
        Parent?.FailMessage = field;
      }
    }

    public Exception Exception
    {
      get;
      set
      {
        field = value;
        Parent?.Exception = field;
      }
    }

    public StackTrace StackTrace
    {
      get;
      set
      {
        field = value;
        Parent?.StackTrace = field;
      }
    }

    public int TestCount
    {
      get
      {
        int count = 0;
        foreach (TestData data in children)
        {
          if (data.TestCase is ITestFunction)
          {
            count++;
          }
          count += data.TestCount;
        }
        return count;
      }
    }

    public void AddChild(TestData entry)
    {
      entry.Parent = this;
      children.Add(entry);
    }

    public void Start()
    {
      stopwatch.Restart();
    }

    public void End()
    {
      stopwatch.Stop();
      Duration = new Benchmark.Result(stopwatch, iterations: 1, Benchmark.Measurement.Milliseconds);
    }

    public void Reset()
    {
      Status = Status.NotRun;
      TestContext = null;
      FailLabel = null;
      FailMessage = null;
      Exception = null;
      StackTrace = null;
      foreach (TestData entry in children)
      {
        entry.Reset();
      }
    }

    void IDataRow<ExplorerColumn>.Draw(Rect rect, ExplorerColumn column)
    {
      column.Draw(rect, this);
    }
  }
}