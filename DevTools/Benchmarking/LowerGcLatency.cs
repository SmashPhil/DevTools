using System;
using System.Runtime;

namespace DevTools.Benchmarking;

internal readonly struct LowerGcLatency : IDisposable
{
	private readonly GCLatencyMode prevLatency;

	public LowerGcLatency(GCLatencyMode latency)
	{
		prevLatency = GCSettings.LatencyMode;
		GCSettings.LatencyMode = latency;
	}

	void IDisposable.Dispose()
	{
		GCSettings.LatencyMode = prevLatency;
	}
}