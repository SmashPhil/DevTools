using System;
using DevTools.Testing;
using JetBrains.Annotations;
using UnityEngine.Assertions;
using Verse;

namespace DevTools;

[PublicAPI]
public static class DevLog
{
  private static Logger logger;

  public static void Write(string message)
  {
    logger?.Write(message);
  }

  public static void WriteLine()
  {
    logger?.Write(string.Empty);
  }

  public static void WriteVerbose(string message)
  {
    logger?.WriteVerbose(message);
  }

  public static void Flush()
  {
    logger?.Flush();
  }

  public static void EnableLogger([NotNull] Logger.Config config)
  {
    Assert.IsNotNull(config.writer);
    logger = (Logger)GenGeneric.InvokeStaticGenericMethod(
      typeof(Logger), config.writer, nameof(Logger.Create), config);
  }

  public static void DisableLogger()
  {
    logger?.Flush();
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