using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace DevTools.Testing;

internal sealed class TcpBridge : ITestRecorder, IDisposable
{
  private readonly int port;
  private readonly string sessionId;

  private TcpClient client;
  private StreamWriter writer;
  private bool disposed;

  public TcpBridge(int port, string sessionId)
  {
    this.sessionId = sessionId;
    this.port = port;
  }

  void ITestRecorder.SendResult(ITestFunction function)
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

  void ITestRecorder.Start()
  {
    client = new TcpClient();
    client.Connect(IPAddress.Loopback, port);
    writer = new StreamWriter(client.GetStream()) { AutoFlush = true };
  }

  void ITestRecorder.Stop(long elapsedMilliseconds)
  {
    writer.WriteLine(JsonSerializer.Serialize(new TestMessage
    {
      SessionId = sessionId,
      MessageType = MessageType.SessionComplete,
      Duration = elapsedMilliseconds
    }));
    Dispose();
  }

  public void Dispose()
  {
    if (disposed)
      return;

    client.Dispose();
    writer.Dispose();
    disposed = true;
  }
}
