using System;
using System.Diagnostics;
using Verse;

namespace DevTools.Benchmarking;

internal class Runner
{
  public const int TestInterval = 5;
  public const int EstimateIterations = 1000;
  public const int Iterations = EstimateIterations * TestInterval;

  private readonly IntPtr funcPtr;
	private readonly IntPtr noOpPtr;

	private readonly TestHarness harness;
	private readonly TestHarness noOpHarness;

	public Runner(IntPtr funcPtr, IntPtr noOpPtr, TestHarness harness, TestHarness noOpHarness)
	{
		this.funcPtr = funcPtr;
		this.noOpPtr = noOpPtr;
		this.harness = harness;
		this.noOpHarness = noOpHarness;
	}

	public Benchmark.Measurement Measurement { get; internal set; } = Benchmark.Measurement.Auto;

	public Benchmark.Result Execute()
	{
		// Elevate process and thread priority
		using NormalizeScheduling ns = new();

		// Warmup
		harness.Invoke();
		noOpHarness.Invoke();

		(int sampleSize, int partitions) = EstimateSampleSize(funcPtr);
		Benchmark.GetPartitionedArrays(sampleSize, partitions, out int[] thresholds, out long[] overhead,
			out long[] results);

		// Do a pass right before we start measuring
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();

		Measure(noOpPtr, noOpHarness, sampleSize, partitions, thresholds, overhead);
		Measure(funcPtr, harness, sampleSize, partitions, thresholds, results);

		for (int i = 0; i < partitions; i++)
			results[i] -= overhead[i];

		return new Benchmark.Result(results, sampleSize, Measurement);
	}

	private static (int sampleSize, int partitions) EstimateSampleSize(IntPtr funcPtr)
	{
		var estimateHarness = TestHarness.Create(funcPtr, TestInterval);
		estimateHarness.Invoke(); // warmup

		// Make sure we're not jumping into an expensive estimation, clamp sample size if so
		Stopwatch watch = Stopwatch.StartNew();
		estimateHarness.Invoke();
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
    {
      estimateHarness.Invoke();
    }
		watch.Stop();
    long ticks = watch.ElapsedTicks / Iterations;
    return ticks > 0 ? Benchmark.GetSampleSize(ticks) : Benchmark.GetMicroBenchmarkSampleSize();
  }

	private static void Measure(IntPtr funcPtr, TestHarness harness, int sampleSize, int partitions, int[] thresholds,
		long[] ticks)
	{
		int batches = sampleSize / harness.UnrollFactor;
		int sampleIdx = 0;
		int callCount = 0;

		long previous = 0;
		int remainder = sampleSize - batches * harness.UnrollFactor;

		TestHarness tailHarness = remainder > 0 ? TestHarness.Create(funcPtr, remainder) : null;
		// Warmup
		tailHarness?.Invoke();

		Stopwatch watch = Stopwatch.StartNew();
		if (batches > 0)
		{
			// Unroll loop for longer tests
			for (int i = 0; i < batches; i++)
			{
				harness.Invoke();
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
			tailHarness!.Invoke();
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