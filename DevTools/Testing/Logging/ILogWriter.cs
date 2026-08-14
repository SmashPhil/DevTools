using System;
using JetBrains.Annotations;

namespace DevTools.Testing;

/// <summary>
/// Defines an output for test log messages.
/// </summary>
[PublicAPI]
public interface ILogWriter : IDisposable
{
  /// <summary>
  /// Invoked after init of the logger.
  /// </summary>
  /// <remarks>
  /// Example: Time stamp the top of the log before the runner starts logging.
  /// </remarks>
  void PostInit();

  /// <summary>
  /// Flushes buffered log messages to the output.
  /// </summary>
  void Flush();

  /// <summary>
  /// Writes a message to the log output.
  /// </summary>
  /// <param name="message">Message to write.</param>
  void WriteLine(string message);
}
