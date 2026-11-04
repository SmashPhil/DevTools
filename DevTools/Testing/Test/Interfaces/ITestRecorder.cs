using JetBrains.Annotations;

namespace DevTools.Testing;

[PublicAPI]
public interface ITestRecorder
{
  void Start();

  void Stop(long elapsedMilliseconds);

  void SendResult(ITestFunction function);
}
