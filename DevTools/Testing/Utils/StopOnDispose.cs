using System;
using System.Diagnostics;

namespace DevTools.Testing;

public readonly struct StopOnDispose : IDisposable
{
	private readonly Stopwatch stopwatch;

	public StopOnDispose(Stopwatch stopwatch)
	{
		this.stopwatch = stopwatch;
		stopwatch.Restart();
	}

	void IDisposable.Dispose()
	{
		stopwatch.Stop();
	}
}