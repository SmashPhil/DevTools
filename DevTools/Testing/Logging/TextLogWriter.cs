using System;
using System.Globalization;
using System.IO;
using JetBrains.Annotations;

namespace DevTools.Testing;

[UsedImplicitly]
internal sealed class TextLogWriter : ILogWriter
{
  private readonly FileStream fileStream;
  private readonly StreamWriter writer;

  public TextLogWriter(Logger logger)
  {
    FileMode mode = FileMode.Append;
    if (logger.IsOwner)
    {
      // Creates or clears log file, we can immediately close it
      // since we want to open with StreamWriter with append mode.
      mode = FileMode.Create;
    }
    fileStream = new FileStream(logger.LogConfig.FullPath, mode, FileAccess.Write, FileShare.ReadWrite);
    writer = new StreamWriter(fileStream);
  }

  void IDisposable.Dispose()
  {
    fileStream.Dispose();
  }

  void ILogWriter.PostInit()
  {
    writer.WriteLine($"{DateTime.Now.ToString("g", DateTimeFormatInfo.CurrentInfo)}");
    writer.WriteLine("");
  }

  void ILogWriter.Flush()
  {
    writer.Flush();
  }

  void ILogWriter.WriteLine(string message)
  {
    writer.WriteLine(message);
  }
}
