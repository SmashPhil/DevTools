using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace DevTools.Benchmarking;

internal readonly struct NormalizeScheduling : IDisposable
{
	//private readonly IntPtr prevAffinity;
	private readonly Process process;
	private readonly ProcessPriorityClass processPriority;
	private readonly ThreadPriority threadPriority;

	public NormalizeScheduling()
	{
		process = Process.GetCurrentProcess();
		//prevAffinity = process.ProcessorAffinity;
		//nint mask = 1 << cpuIndex;
		//process.ProcessorAffinity = mask;
		//Thread.BeginThreadAffinity();

		processPriority = process.PriorityClass;
		process.PriorityClass = ProcessPriorityClass.High;
		threadPriority = Thread.CurrentThread.Priority;
		Thread.CurrentThread.Priority = ThreadPriority.Highest;
	}

	void IDisposable.Dispose()
	{
		process.PriorityClass = processPriority;
		Thread.CurrentThread.Priority = threadPriority;
	}

	private static IntPtr AdjustAffinity(IntPtr affinity)
	{
		int mask = (1 << Environment.ProcessorCount) - 1;
		return RuntimeInformation.OSArchitecture is Architecture.X64 or Architecture.Arm64 ?
			new IntPtr(affinity.ToInt64() & mask) :
			new IntPtr(affinity.ToInt32() & mask);
	}
}