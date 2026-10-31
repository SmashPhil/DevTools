using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace DevTools.Testing;

internal class TcpBridge : IDisposable
{
  private readonly string sessionId;
  private readonly TcpClient client;
  private readonly StreamWriter writer;
  private readonly Stopwatch stopwatch;

  private bool disposed;

  public TcpBridge(int port, string sessionId, Stopwatch stopwatch)
  {
    this.sessionId = sessionId;
    this.stopwatch = stopwatch;
    client = new TcpClient();
    client.Connect(IPAddress.Loopback, port);
    writer = new StreamWriter(client.GetStream()) { AutoFlush = true };
  }

  public void SendResult(ITestFunction function)
  {
    ITestGroup group = Test.GetEntry(function);
    writer.WriteLine(JsonSerializer.Serialize(new TestMessage
    {
      SessionId = sessionId,
      MessageType = MessageType.TestResult,
      Name = function.Name,
      FullName = function.FullName,
      Status = function.Status,
      Duration = group.Duration.ElapsedMilliseconds,
      Message = group.FailMessage,
      StackTrace = group.StackTrace,
      Source = function.Module.Source
    }));
  }

  private void Stop()
  {
    writer.WriteLine(JsonSerializer.Serialize(new TestMessage
    {
      SessionId = sessionId,
      MessageType = MessageType.SessionComplete,
      Duration = stopwatch.ElapsedMilliseconds
    }));
  }

  public void Dispose()
  {
    if (disposed)
      return;

    Stop();
    client.Dispose();
    writer.Dispose();
    disposed = true;
  }
}
