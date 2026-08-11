using System;
using System.IO;
using System.Threading;
using JetBrains.Annotations;
using UnityEngine;
using Verse;

namespace DevTools.Testing;

[PublicAPI]
public class Logger : IDisposable
{
  private readonly FileInfo file;

  private ILogWriter writer;

  private Mutex writerMutex;
  private Mutex ownerMutex;

  private Logger(Config config)
  {
    LogConfig = config;
    ownerMutex = new Mutex(false, $"OwnerMutex_{Config.LogFileName}");
    writerMutex = new Mutex(false, $"LoggerMutex_{Config.LogFileName}");
    file = new FileInfo(config.FullPath);
    IsOwner = ownerMutex.WaitOne(0);
  }

  public bool IsOwner { get; }

  public bool Disposed { get; private set; }

  public Config LogConfig { get; private set; }

  public static Logger Create<T>(Config config) where T : ILogWriter
  {
    Logger logger = new(config);
    logger.writer = (ILogWriter)Activator.CreateInstance(typeof(T), args: logger);
    logger.InitializeWriter();
    return logger;
  }

  private void InitializeWriter()
  {
    file.Refresh();
    if (!file.Exists || file.Length >= LogConfig.maxFileSize)
      return;

    using MutexLock ml = new(writerMutex);
    writer.PostInit();
  }

  public void Flush()
  {
    using MutexLock ml = new(writerMutex);
    if (Disposed)
      return;

    writer.Flush();
  }

  public void Write(string message)
  {
    if (Disposed)
      return;

    file.Refresh();
    if (!file.Exists || file.Length >= LogConfig.maxFileSize)
      return;

    using MutexLock ml = new(writerMutex);
    writer.WriteLine(message);
  }

  public void WriteVerbose(string message)
  {
    if (LogConfig.verboseLogging)
    {
      Write(message);
    }
  }

  public void Dispose()
  {
    if (!Disposed)
    {
      using (new MutexLock(writerMutex))
      {
        writer?.Dispose();
        Disposed = true;
      }
      writerMutex.Dispose();
      writerMutex = null;
    }
    
    try
    {
      if (IsOwner)
      {
        ownerMutex.ReleaseMutex();
      }
    }
    finally
    {
      ownerMutex.Dispose();
    }
  }

  private readonly struct MutexLock : IDisposable
  {
    private readonly bool taken;
    private readonly Mutex mutex;

    public MutexLock(Mutex mutex)
    {
      this.mutex = mutex;
      taken = mutex.WaitOne();
    }

    public void Dispose()
    {
      if (taken)
        mutex.ReleaseMutex();
    }
  }

  [PublicAPI]
  public class Config
  {
    public const string LogFileName = "Test.log";

    private const long MegaByteConversion = 1024 * 1024;
    public const long DefaultFileLimit = 10 * MegaByteConversion;

    public long maxFileSize = 10 * MegaByteConversion; // 10 mb
    public bool verboseLogging = true;
    public bool shareLogFile = true;
    [LoadAlias("filePath")]
    public string folder = Application.persistentDataPath;

    public Type writer = typeof(TextLogWriter);
    public Type report;

    public string FullPath => Path.Combine(folder, LogFileName);

    public void PostLoad()
    {
      if (writer == null || !typeof(ILogWriter).IsAssignableFrom(writer))
      {
        Log.Error($"Invalid log writer type {writer?.FullName ?? "NULL"}. Type must inherit from ILogWriter.");
        writer = typeof(TextLogWriter);
      }

      if (report != null && !typeof(ITestReport).IsAssignableFrom(report))
      {
        Log.Error($"Invalid log reporter type {report?.FullName ?? "NULL"}. Type must inherit from ITestReport.");
      }
    }
  }
}