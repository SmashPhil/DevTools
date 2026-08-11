using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using UnityEngine;
using Verse;

// ReSharper disable ExtractCommonBranchingCode

namespace DevTools.Benchmarking;

/// <summary>
/// Benchmarking util for comparing expensive operations.
/// </summary>
/// <remarks>
/// Note: Only accurate if used for operations that are many times more 
/// expensive than a single method call. This should not be used for 
/// micro-benchmarking as results will not be accurate.
/// </remarks>
public static class Benchmark
{
	private static void ShowWarnings()
	{
		// Can't log as an error since Ludeon's message window is not thread safe and will crash the game.
#if RELEASE
		if (Debugger.IsAttached)
		{
			Log.WarningOnce(
				"Benchmarks should not be executed with debugger attached. The results will be wildly inaccurate.",
				"Benchmark.DebuggerAttached".GetHashCode());
		}
#endif
	}

	internal static void GetPartitionedArrays(int sampleSize, int partitions, out int[] thresholds, out long[] overhead,
		out long[] results)
	{
		thresholds = new int[partitions];
		for (int i = 0; i < partitions; i++)
			thresholds[i] = Mathf.CeilToInt(sampleSize * (float)(i + 1) / partitions);
		overhead = new long[partitions];
		results = new long[partitions];
	}

	internal static (int sampleSize, int partitions) GetSampleSize(long ticks)
	{
		return Result.ToMicroseconds(ticks) switch
		{
			> 100_000 => (sampleSize: 100, partitions: 10),
			> 10_000  => (sampleSize: 1_000, partitions: 10),
			> 1_000   => (sampleSize: 10_000, partitions: 20),
			> 100     => (sampleSize: 100_000, partitions: 50),
			> 10 => (sampleSize: 1_000_000, partitions: 100),
      // With 1k iterations for estimate this is unlikely to occur. Requires heavy amortization to get even
      // remotely close to usable results. 
      _  => (sampleSize: 100_000_000, partitions: 100)
		};
	}

  internal static (int sampleSize, int partitions) GetMicroBenchmarkSampleSize()
  {
    // For microbenchmarking. We're closer to Stopwatch granularity, we need more aggression on sample size
    // to drown out overhead from Stopwatch.
    return (sampleSize: 1_000_000_000, partitions: 10);
  }

  /// <returns>
  /// Time to execute <paramref name="funcPtr"/> 
  /// </returns>
  /// <param name="funcPtr">Function to execute each iteration.</param>
  /// <param name="measurement">Measurement of accuracy for benchmark results.</param>
  [MethodImpl(MethodImplOptions.NoOptimization)]
	public static unsafe Result Run(IntPtr funcPtr, Measurement measurement = Measurement.Auto)
	{
		const int UnrollFactor = 16;

		ShowWarnings();

		IntPtr noOpPtr = (IntPtr)(delegate*<void>)&NoOp;

		var harness = TestHarness.Create(funcPtr, UnrollFactor);
		var noOpHarness = TestHarness.Create(noOpPtr, UnrollFactor);

		Runner runner = new(funcPtr, noOpPtr, harness, noOpHarness)
		{
			Measurement = measurement
		};
		return runner.Execute();

		// Stub for function invocation and loop overhead
		[MethodImpl(MethodImplOptions.NoInlining)]
		static void NoOp()
		{
		}
	}

	/// <returns>
	/// Time to execute <paramref name="funcPtr"/> with return type <typeparamref name="R"/>
	/// </returns>
	/// <remarks>Return from benchmark methods to prevent JIT dead code optimizations.</remarks>
	/// <param name="funcPtr">Function to execute each iteration.</param>
	/// <param name="measurement">Measurement of accuracy for benchmark results.</param>
	[MethodImpl(MethodImplOptions.NoOptimization)]
	public static unsafe Result Run<R>(IntPtr funcPtr, Measurement measurement = Measurement.Auto)
	{
		const int UnrollFactor = 16;

		ShowWarnings();

		IntPtr noOpPtr = (IntPtr)(delegate*<R>)&NoOp;

		var harness = TestHarness.Create<R>(funcPtr, UnrollFactor);
		var noOpHarness = TestHarness.Create<R>(noOpPtr, UnrollFactor);

		Runner runner = new(funcPtr, noOpPtr, harness, noOpHarness)
		{
			Measurement = measurement
		};
		return runner.Execute();

		// Stub for function invocation and loop overhead
		[MethodImpl(MethodImplOptions.NoInlining)]
		static R NoOp()
		{
			return default;
		}
	}

	/// <returns>
	/// Time to execute <paramref name="funcPtr"/> 
	/// </returns>
	/// <param name="funcPtr">Function to execute each iteration.</param>
	/// <param name="context">Object passed in with each function call.</param>
	/// <param name="measurement">Measurement of accuracy for benchmark results.</param>
	public static unsafe Result Run<T>(IntPtr funcPtr, T context,
		Measurement measurement = Measurement.Auto) where T : struct
	{
		const int UnrollFactor = 16;

		ShowWarnings();

		IntPtr noOpPtr = (IntPtr)(delegate*<ref T, void>)&NoOp;

		var harness = TestHarnessWithContext<T>.Create(funcPtr, UnrollFactor);
		var noOpHarness = TestHarnessWithContext<T>.Create(noOpPtr, UnrollFactor);

		RunnerWithContext<T> runner = new(funcPtr, noOpPtr, harness, noOpHarness)
		{
			Measurement = measurement
		};
		return runner.Execute(ref context);

		// Stub for function invocation and loop overhead
		[MethodImpl(MethodImplOptions.NoInlining)]
		static void NoOp(ref T _)
		{
		}
	}

	/// <returns>
	/// Time to execute <paramref name="funcPtr"/> with return type <typeparamref name="R"/>
	/// </returns>
	/// <remarks>Return from benchmark methods to prevent JIT dead code optimizations.</remarks>
	/// <param name="funcPtr">Function pointer to invoke each iteration.</param>
	/// <param name="context">Object passed in with each function call.</param>
	/// <param name="measurement">Measurement of accuracy for benchmark results.</param>
	public static unsafe Result Run<T, R>(IntPtr funcPtr, T context,
		Measurement measurement = Measurement.Auto) where T : struct
	{
		const int UnrollFactor = 16;

		ShowWarnings();

		IntPtr noOpPtr = (IntPtr)(delegate*<ref T, R>)&NoOp;

		// Warmup
		DeadCodeHelper.KeepAliveReadOnly<R>(default);

		var harness = TestHarnessWithContext<T>.Create<R>(funcPtr, UnrollFactor);
		var noOpHarness = TestHarnessWithContext<T>.Create<R>(noOpPtr, UnrollFactor);

		RunnerWithContext<T> runner = new(funcPtr, noOpPtr, harness, noOpHarness)
		{
			Measurement = measurement
		};
		return runner.Execute(ref context);

		// Stub for function invocation and loop overhead
		[MethodImpl(MethodImplOptions.NoInlining)]
		static R NoOp(ref T _)
		{
			return default;
		}
	}

	/// <returns>
	/// Time to execute <paramref name="action"/> 
	/// </returns>
	/// <remarks>
	/// Uses delegate for benchmarking non-static functions or functions that use closure. This implementation will
	/// have lower accuracy from extra indirection and any closure from the caller. Only use for rough estimates on
	/// higher invocation cost methods.
	/// </remarks>
	/// <param name="action">Function to execute each iteration.</param>
	/// <param name="measurement">Measurement of accuracy for benchmark results.</param>
	public static Result Run(Action action, Measurement measurement = Measurement.Auto)
	{
		return Run(Marshal.GetFunctionPointerForDelegate(action), measurement);
	}

	public static string MeasurementSuffix(Measurement measurement)
	{
		return measurement switch
		{
			Measurement.Seconds      => "s",
			Measurement.Milliseconds => "ms",
			Measurement.Microseconds => "\u00b5s",
			Measurement.Nanoseconds  => "ns",
			// Auto should never be retained, it should be auto converted when Result object is created
			Measurement.Auto => throw new InvalidOperationException(nameof(measurement)),
			_                => throw new NotImplementedException(),
		};
	}

	public enum Measurement
	{
		Auto,
		Seconds,
		Milliseconds,
		Microseconds,
		Nanoseconds,
	}

	[PublicAPI]
	public readonly record struct Result
	{
		// ReSharper disable ConvertToAutoProperty
		private static readonly Measurement[] OrderedMeasurements =
		[
			Measurement.Seconds, Measurement.Milliseconds,
			Measurement.Microseconds, Measurement.Nanoseconds
		];

		private readonly int decimalPlaces;

		private readonly Measurement measurement;
		private readonly int samples;
		private readonly int partitions;

		private readonly double total;
		private readonly double mean;
		private readonly double median;
		private readonly double stdDev;

		public Result(long[] ticks, int samples,
			Measurement measurement = Measurement.Auto,
			int decimalPlaces = 4)
		{
			this.decimalPlaces = decimalPlaces;
			this.samples = samples;
			this.partitions = ticks.Length;

			total = ticks.Sum();

			int callsPerPartition = samples / ticks.Length;
			mean = ticks.Mean() / callsPerPartition;
			median = ticks.Median() / callsPerPartition;
			stdDev = ticks.StdDev() / callsPerPartition;

			this.measurement = measurement == Measurement.Auto ? PreferredMeasurement() : measurement;
			total = Converted(total, this.measurement);
			mean = Converted(mean, this.measurement);
			median = Converted(median, this.measurement);
			stdDev = Converted(stdDev, this.measurement);
		}

		public Result(Stopwatch stopwatch, int iterations, Measurement measurement = Measurement.Auto,
			int decimalPlaces = 4) : this([stopwatch.ElapsedTicks], iterations, measurement,
			decimalPlaces)
		{
		}

		public int Samples => samples;

		public int Partitions => partitions;

		public double Total => total;

		public double Mean => mean;

		public double Median => median;

		public double StdDev => stdDev;

		public string Formatted(double value)
    {
      return
        $"{value.ToString($"0.{new string('0', decimalPlaces)}")} {MeasurementSuffix(measurement)}";
    }

		public override string ToString()
		{
			return
				$"Sample={samples} | Total={Formatted(Total)} | Mean={Formatted(Mean)}";
		}

		private Measurement PreferredMeasurement()
		{
			foreach (Measurement curMeasurement in OrderedMeasurements)
			{
				double mTotal = Converted(Total, curMeasurement);
				double mMean = Converted(Mean, curMeasurement);
				double mMedian = Converted(Median, curMeasurement);
				if (mTotal >= 0.1 && mMean >= 0.1 && mMedian >= 0.1)
					return curMeasurement;
			}
			return Measurement.Nanoseconds;
		}

		private static double Converted(double ticks, Measurement measurement)
		{
			return measurement switch
			{
				Measurement.Seconds      => ToSeconds(ticks),
				Measurement.Milliseconds => ToMilliseconds(ticks),
				Measurement.Microseconds => ToMicroseconds(ticks),
				Measurement.Nanoseconds  => ToNanoseconds(ticks),
				Measurement.Auto         => throw new InvalidOperationException(),
				_                        => throw new NotImplementedException(nameof(Measurement)),
			};
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static double ToSeconds(double ticks)
		{
			return ticks / Stopwatch.Frequency;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static double ToMilliseconds(double ticks)
		{
			return ticks * 1000 / Stopwatch.Frequency;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static double ToMicroseconds(double ticks)
		{
			return ticks * 1_000_000 / Stopwatch.Frequency;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static double ToNanoseconds(double ticks)
		{
			return ticks * 1_000_000_000 / Stopwatch.Frequency;
		}
	}
}