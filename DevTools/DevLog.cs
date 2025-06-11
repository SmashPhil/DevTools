using System;
using System.Globalization;
using DevTools.UnitTesting;
using JetBrains.Annotations;
using UnityEngine.Assertions;

namespace DevTools;

[PublicAPI]
public static class DevLog
{
  private static Logger logger;

  public static void Write(string message)
  {
    Assert.IsNotNull(logger);
    logger.Write(message);
  }

  public static void WriteLine()
  {
    logger.WriteLine();
  }

  public static void WriteVerbose(string message)
  {
    logger.WriteVerbose(message);
  }

  public static void EnableLogger([NotNull] Logger.Config config)
  {
    logger = new Logger(config);
    Write(
      $"{DateTime.Now.ToString("g", DateTimeFormatInfo.CurrentInfo)}{Environment.NewLine}{Environment.NewLine}");
    WriteLine();
  }

  public static void DisableLogger()
  {
    logger?.Dispose();
    logger = null;
  }

  public readonly struct Enabler : IDisposable
  {
    public Enabler([NotNull] Logger.Config config)
    {
      EnableLogger(config);
    }

    void IDisposable.Dispose()
    {
      DisableLogger();
    }
  }
}