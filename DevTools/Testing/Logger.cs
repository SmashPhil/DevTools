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

	private bool owner;
	private Mutex writerMutex;

	public Logger(Config config)
	{
		this.config = config;

		writerMutex = new Mutex(false, $"LoggerMutex_{Config.LogFileName}");
		FileInfo initFile = new(config.InitFilePath);
		FileMode mode = FileMode.Append;
		if (!initFile.Exists)
		{
			owner = true;
			// Creates init file to lock log file from being cleared from child processes.
			using FileStream initStream = File.Create(config.InitFilePath);
			initStream.Close();

			// Creates or clears log file, we can immediately close it since
			// we want to open with StreamWriter with append mode.
			mode = FileMode.Create;
		}
		fileStream = new FileStream(config.FullPath, mode, FileAccess.Write, FileShare.ReadWrite);
		file = new FileInfo(config.FullPath);
		writer = new StreamWriter(fileStream);
	}

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
		if (Disposed || !file.Exists)
			return;
		if (file.Length >= config.maxFileSize)
			return;

		using MutexLock ml = new(writerMutex);
		if (Disposed)
			return;
		writer.WriteLine(message);
	}

	public void WriteVerbose(string message)
	{
		if (config.verboseLogging)
			Write(message);
	}

	public void WriteLine()
	{
		using MutexLock ml = new(writerMutex);
		if (Disposed)
			return;
		writer.WriteLine();
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

		if (owner && File.Exists(config.InitFilePath))
		{
			File.Delete(config.InitFilePath);
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
		public const string InitFileName = LogFileName + ".lock";

		private const long MegaByteConversion = 1024 * 1024;
		public const long DefaultFileLimit = 10 * MegaByteConversion;

		public long maxFileSize = 10 * MegaByteConversion; // 10 mb
		public bool verboseLogging = true;
		public bool shareLogFile = true;
		public string filePath = Application.persistentDataPath;

		public string FullPath => Path.Combine(filePath, LogFileName);

		public string InitFilePath => Path.Combine(filePath, InitFileName);
	}
}