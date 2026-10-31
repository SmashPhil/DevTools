using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace DevTools.TestAdapter;

internal sealed class TestSession : IDisposable
{
  private const string TestListFileName = "DevTools_TestList-{0}.txt";

  private readonly RunSettings settings;
  private readonly string packageId;
  private readonly IRunContext runContext;
  private readonly ProcessStartInfo startInfo;

  private readonly List<string> arguments = [];
  private readonly Dictionary<string, TestCase> testsByName = [];
  private Process process;
  private CancellationTokenSource cts;
  
  private event Action OnSessionEnd;

  public TestSession(RunSettings settings, IRunContext runContext)
  {
    this.settings = settings;
    packageId = settings.PackageId;
    this.runContext = runContext;
    startInfo = new ProcessStartInfo
    {
      FileName = settings.ExePath,
      WorkingDirectory = Path.GetDirectoryName(settings.ExePath)!,
      UseShellExecute = true,
      CreateNoWindow = settings.Mode is RunSettings.WindowMode.NoWindow or RunSettings.WindowMode.Headless,
      WindowStyle = settings.Mode is RunSettings.WindowMode.Minimized ?
        ProcessWindowStyle.Minimized :
        ProcessWindowStyle.Normal
    };
  }

  public Guid SessionId { get; } = Guid.NewGuid();

  public void LoadTests(IEnumerable<TestCase> tests)
  {
    testsByName.Clear();
    string tempFile = Path.Combine(Path.GetTempPath(), string.Format(TestListFileName, SessionId));
    using var stream = File.CreateText(tempFile);
    foreach (TestCase test in tests)
    {
      stream.WriteLine(test.DisplayName);
      testsByName[test.DisplayName] = test;
    }
    arguments.Add($"--test-file {tempFile}");
    OnSessionEnd += () => File.Delete(tempFile);
  }

  public void AddArg(string arg)
  {
    arguments.Add(arg);
  }

  public void Run(IFrameworkHandle handle)
  {
    RunAsync(handle).GetAwaiter().GetResult();
  }

  public async Task RunAsync(IFrameworkHandle handle)
  {
    cts = new CancellationTokenSource();
    using TcpListener listener = new(IPAddress.Loopback, port: 0);
    listener.Start();

    string sessionId = SessionId.ToString();
    int port = ((IPEndPoint)listener.LocalEndpoint).Port;
    string args = GetCommandLineArgs(port, sessionId);
    if (runContext.IsBeingDebugged)
    {
      int pid = handle.LaunchProcessWithDebuggerAttached(startInfo.FileName, startInfo.WorkingDirectory, arguments: args,
        environmentVariables: null);
      process = Process.GetProcessById(pid);
    }
    else
    {
      if (process != null)
        throw new InvalidOperationException("Trying to launch session while one is already ongoing.");

      startInfo.Arguments = args;
      process = Process.Start(startInfo);
    }

    if (process is null)
    {
      await Console.Error.WriteLineAsync("Unable to start process");
      return;
    }

    using TcpClient client = await listener.AcceptTcpClientAsync(cts.Token);
    using StreamReader reader = new(client.GetStream());
    while (await reader.ReadLineAsync(cts.Token) is { } payload)
    {
      var testEvent = JsonSerializer.Deserialize<TestMessage>(payload);
      if (testEvent.SessionId != sessionId)
        throw new InvalidOperationException("Receiving data from wrong session id.");

      if (testEvent.MessageType is MessageType.SessionComplete)
        break;

      if (!testsByName.TryGetValue(testEvent.FullName, out TestCase testCase))
      {
        testCase = new TestCase(testEvent.FullName, new Uri(AdapterInfo.ExecutorUri), testEvent.Source)
        {
          DisplayName = testEvent.Name
        };
      }
      handle.RecordResult(ToTestResult(testEvent, testCase));
    }
    await process.WaitForExitAsync(cts.Token);
  }

  private static TestResult ToTestResult(TestMessage testEvent, TestCase testCase)
  {
    TestOutcome outcome = testEvent.Status switch
    {
      Status.Passed => TestOutcome.Passed,
      Status.Failed => TestOutcome.Failed,
      Status.Skipped or Status.Canceled => TestOutcome.Skipped,
      _ => throw new NotImplementedException(testEvent.Status.ToString())
    };
    return new TestResult(testCase)
    {
      Outcome = outcome,
      Duration = TimeSpan.FromMilliseconds(testEvent.Duration),
      ErrorMessage = testEvent.Message,
      ErrorStackTrace = testEvent.StackTrace
    };
  }

  private string GetCommandLineArgs(int port, string sessionId)
  {
    StringBuilder argBuilder = new();
    argBuilder.Append($" --pid \"{packageId}\"");
    argBuilder.Append(" --test");
    argBuilder.Append($" --port {port} --session {sessionId}");
    argBuilder.Append(" --exit");
    switch (settings.Mode)
    {
      case RunSettings.WindowMode.NoWindow:
        argBuilder.Append(" -NoWindow");
        break;
      case RunSettings.WindowMode.Headless:
        argBuilder.Append(" -Headless");
        break;
      case RunSettings.WindowMode.Minimized:
      case RunSettings.WindowMode.Windowed:
        break;
    }
    return $"{argBuilder} {string.Join(" ", arguments)}";
  }

  public void Stop()
  {
    if (process is not { HasExited: false })
      return;

    cts.Cancel();
  }

  public void Kill()
  {
    if (process is not { HasExited: false })
      return;

    const int KillTimeOutMs = 10000; // ms

    process.Kill();
    if (!process.WaitForExit(KillTimeOutMs))
    {
      Console.Error.WriteLine("Failed to kill process, it may be orphaned.");
    }
  }

  public void Dispose()
  {
    if (process is { HasExited: false })
    {
      Kill();
    }
    OnSessionEnd?.Invoke();
    cts?.Dispose();
    cts = null;
    process?.Dispose();
    process = null;
  }
}
