using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace DevTools.TestAdapter;

internal sealed class TestSession : IDisposable
{
  private readonly string packageId;
  private readonly string testListPath;
  private readonly ProcessStartInfo startInfo;

  private Process process;
  private CancellationTokenSource cts;

  public TestSession(string packageId, string testListPath, string exePath)
  {
    this.packageId = packageId;
    this.testListPath = testListPath;
    startInfo = new ProcessStartInfo
    {
      FileName = exePath,
      WorkingDirectory = Path.GetDirectoryName(exePath)!,
      UseShellExecute = true,
      CreateNoWindow = false,
      WindowStyle = ProcessWindowStyle.Normal
    };
  }

  public void Dispose()
  {
    if (process is { HasExited: false })
    {
      Kill();
    }
    cts?.Dispose();
    cts = null;
    process?.Dispose();
    process = null;
  }

  public void Run(IEnumerable<TestCase> tests, IFrameworkHandle handle)
  {
    RunAsync(tests, handle).GetAwaiter().GetResult();
  }

  public async Task RunAsync(IEnumerable<TestCase> tests, IFrameworkHandle handle)
  {
    cts = new CancellationTokenSource();
    using TcpListener listener = new(IPAddress.Loopback, port: 0);
    listener.Start();

    string sessionId = Guid.NewGuid().ToString();
    int port = ((IPEndPoint)listener.LocalEndpoint).Port;
    LaunchGame(port, sessionId);

    if (process is null)
    {
      await Console.Error.WriteLineAsync("Unable to start process");
      return;
    }

    using TcpClient client = await listener.AcceptTcpClientAsync(cts.Token);
    using StreamReader reader = new(client.GetStream());
    var testsByName = tests.ToDictionary(test => test.DisplayName, test => test);

    while (await reader.ReadLineAsync(cts.Token) is { } payload)
    {
      var testEvent = JsonSerializer.Deserialize<TestMessage>(payload);
      if (testEvent.SessionId != sessionId)
        throw new InvalidOperationException("Receiving data from wrong session id.");

      if (testEvent.MessageType is MessageType.SessionComplete)
        break;

      if (!testsByName.TryGetValue(testEvent.FullName, out TestCase testCase))
      {
        await Console.Error.WriteLineAsync($"No test found matching {testEvent.FullName}");
        continue;
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

  private void LaunchGame(int port, string sessionId)
  {
    if (process != null)
      throw new InvalidOperationException("Trying to launch session while one is already ongoing.");

    startInfo.ArgumentList.Add("--pid");
    startInfo.ArgumentList.Add(packageId);
    startInfo.ArgumentList.Add("--test");
    startInfo.ArgumentList.Add("--test-list");
    startInfo.ArgumentList.Add(testListPath);
    startInfo.ArgumentList.Add("--exit");
    startInfo.ArgumentList.Add("--port");
    startInfo.ArgumentList.Add(port.ToString());
    startInfo.ArgumentList.Add("--session");
    startInfo.ArgumentList.Add(sessionId);
    startInfo.ArgumentList.Add("-NoWindow");
    process = Process.Start(startInfo);
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
}
