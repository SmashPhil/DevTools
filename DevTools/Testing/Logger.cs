using System;
using System.IO;
using System.Threading;
using JetBrains.Annotations;
using UnityEngine;

namespace DevTools.Testing;

[PublicAPI]
public class Logger : IDisposable
{
	private readonly Config config;
	private readonly FileInfo file;
	private readonly FileStream fileStream;
	private readonly StreamWriter writer;

	private Mutex writerMutex;
	private Mutex ownerMutex;

	public Logger(Config config)
	{
		this.config = config;

		ownerMutex = new Mutex(false, $"OwnerMutex_{Config.LogFileName}");
		writerMutex = new Mutex(false, $"LoggerMutex_{Config.LogFileName}");
		FileMode mode = FileMode.Append;
		if (ownerMutex.WaitOne(0))
		{
			IsOwner = true;
			// Creates or clears log file, we can immediately close it
			// since we want to open with StreamWriter with append mode.
			mode = FileMode.Create;
		}
		fileStream = new FileStream(config.FullPath, mode, FileAccess.Write, FileShare.ReadWrite);
		file = new FileInfo(config.FullPath);
		writer = new StreamWriter(fileStream);
	}

	public bool IsOwner { get; }

	public bool Disposed { get; private set; }

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
		if (!file.Exists || file.Length >= config.maxFileSize)
			return;

		using MutexLock ml = new(writerMutex);
		writer.WriteLine(message);
	}

	public void WriteVerbose(string message)
	{
		if (config.verboseLogging)
		{
			Write(message);
		}
	}

	public void Dispose()
	{
		using (new MutexLock(writerMutex))
		{
			writer?.Dispose();
			fileStream?.Dispose();

			Disposed = true;
		}
		writerMutex.Dispose();

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
		public string filePath = Application.persistentDataPath;

		public string FullPath => Path.Combine(filePath, LogFileName);
	}
}