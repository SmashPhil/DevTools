using System;
using JetBrains.Annotations;

namespace DevTools.Testing;

[PublicAPI]
public interface ILogWriter : IDisposable
{
  void PostInit();

  void Flush();

  void WriteLine(string message);
}
