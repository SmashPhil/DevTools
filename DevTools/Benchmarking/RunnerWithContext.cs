using System;
using System.Diagnostics;
using Verse;

namespace DevTools.Benchmarking;

internal class RunnerWithContext<T>
{
	private readonly IntPtr funcPtr;
	private readonly IntPtr noOpPtr;

	private readonly TestHarnessWithContext<T> harness;
	private readonly TestHarnessWithContext<T> noOpHarness;

	public RunnerWithContext(IntPtr funcPtr, IntPtr noOpPtr, TestHarnessWithContext<T> harness,
		TestHarnessWithContext<T> noOpHarness)
	{
		this.funcPtr = funcPtr;
		this.noOpPtr = noOpPtr;
		this.harness = harness;
		this.noOpHarness = noOpHarness;
	}

	public Benchmark.Measurement Measurement { get; internal set; } = Benchmark.Measurement.Auto;

	public Benchmark.Result Execute(ref T context)
	{
		// Elevate process and thread priority
		using NormalizeScheduling ns = new();

		T warmupContext = context;

		// Warmup
		harness.Invoke(ref context);
		noOpHarness.Invoke(ref warmupContext);

		(int sampleSize, int partitions) = EstimateSampleSize(funcPtr, ref context);
		Benchmark.GetPartitionedArrays(sampleSize, partitions, out int[] thresholds, out long[] overhead,
			out long[] results);

		// Do a pass right before we start measuring
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();

		Measure(noOpPtr, noOpHarness, ref warmupContext, sampleSize, partitions, thresholds, overhead);
		Measure(funcPtr, harness, ref context, sampleSize, partitions, thresholds, results);

		for (int i = 0; i < partitions; i++)
			results[i] -= overhead[i];

		return new Benchmark.Result(results, sampleSize, Measurement);
	}

	private static (int sampleSize, int partitions) EstimateSampleSize(IntPtr funcPtr, ref T context)
	{
		const int TestInterval = 5;
		const int EstimateIterations = 1000;
		const int Iterations = EstimateIterations / TestInterval;

		var estimateHarness = TestHarnessWithContext<T>.Create(funcPtr, TestInterval);
		estimateHarness.Invoke(ref context); // warmup

		// Make sure we're not jumping into an expensive estimation, clamp sample size if so
		Stopwatch watch = Stopwatch.StartNew();
		estimateHarness.Invoke(ref context);
		watch.Stop();

		// Max cutoff is 100ms per iteration.
		if (watch.ElapsedMilliseconds > 100 * TestInterval)
		{
			Log.Warning(
				"Running benchmark on function that takes a long time to execute. To keep results reliable, iterations cannot be lowered further.");
			return (sampleSize: 100, partitions: 10);
		}

		// Estimate good sample size
		watch.Restart();
		for (int i = 0; i < Iterations; i++)
			estimateHarness.Invoke(ref context);
		watch.Stop();
		return Benchmark.GetSampleSize(watch.ElapsedTicks / EstimateIterations);
	}

	private static void Measure(IntPtr funcPtr, TestHarnessWithContext<T> harness,
		ref T context, int sampleSize, int partitions, int[] thresholds, long[] ticks)
	{
		int batches = sampleSize / harness.UnrollFactor;
		int sampleIdx = 0;
		int callCount = 0;

		long previous = 0;
		int remainder = sampleSize - batches * harness.UnrollFactor;

		TestHarnessWithContext<T> tailHarness =
			remainder > 0 ? TestHarnessWithContext<T>.Create(funcPtr, remainder) : null;
		// Warmup
		tailHarness?.Invoke(ref context);

		Stopwatch watch = Stopwatch.StartNew();
		if (batches > 0)
		{
			// Unroll loop for longer tests
			for (int i = 0; i < batches; i++)
			{
				harness.Invoke(ref context);
				callCount += harness.UnrollFactor;

				while (sampleIdx < partitions && callCount >= thresholds[sampleIdx])
				{
					long current = watch.ElapsedTicks;
					ticks[sampleIdx++] = current - previous;
					previous = current;
				}
			}
		}
		if (remainder > 0)
		{
			tailHarness!.Invoke(ref context);
			callCount += remainder;

			while (sampleIdx < partitions && callCount >= thresholds[sampleIdx])
			{
				long current = watch.ElapsedTicks;
				ticks[sampleIdx++] = current - previous;
				previous = current;
			}
		}
		watch.Stop();
	}
}