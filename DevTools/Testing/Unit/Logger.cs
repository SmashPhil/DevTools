using System;
using System.IO;
using JetBrains.Annotations;
using UnityEngine;

namespace DevTools.Testing;

[PublicAPI]
public class Logger : IDisposable
{
	private readonly Config config;
	private readonly FileInfo file;
	private readonly StreamWriter writer;

	private object writerLock = new();

	public Logger(Config config)
	{
		this.config = config;

		// Creates or clears log file, we can immediately close it since
		// we want to open with StreamWriter with append mode.
		using FileStream stream = File.Create(config.FullPath);
		stream.Close();

		file = new FileInfo(config.FullPath);
		writer = new StreamWriter(config.FullPath, append: true);
	}

	public bool Disposed { get; private set; }

	public void Flush()
	{
		lock (writerLock)
		{
			if (Disposed)
				return;
			writer.Flush();
		}
	}

	public void Write(string message)
	{
		if (Disposed || !file.Exists || file.Length >= config.maxFileSize)
			return;

		lock (writerLock)
		{
			if (Disposed)
				return;
			writer.WriteLine(message);
		}
	}

	public void WriteVerbose(string message)
	{
		if (config.verboseLogging)
			Write(message);
	}

	public void WriteLine()
	{
		lock (writerLock)
		{
			writer.WriteLine();
		}
	}

	public void Dispose()
	{
		lock (writerLock)
		{
			Disposed = true;
			writer?.Dispose();
		}
	}

	[PublicAPI]
	public class Config
	{
		private const string LogFileName = "Test.log";

		private const long MegaByteConversion = 1024 * 1024;
		public const long DefaultFileLimit = 10 * MegaByteConversion;

		public long maxFileSize = 10 * MegaByteConversion; // 10 mb
		public bool verboseLogging = true;
		public string filePath = Application.persistentDataPath;

		public string FullPath => Path.Combine(filePath, LogFileName);
	}
}