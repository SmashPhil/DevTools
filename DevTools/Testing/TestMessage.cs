namespace DevTools.Testing;

internal struct TestMessage
{
  public string SessionId { get; init; }

  public MessageType MessageType { get; init; }

  public string FullName { get; init; }

  public Status Status { get; init; }

  public long Duration { get; init; }

  public string Message { get; init; }

  public string StackTrace { get; init; }
}