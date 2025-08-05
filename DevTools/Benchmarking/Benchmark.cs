using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Threading;
using JetBrains.Annotations;
using UnityEngine;
using Verse;
using ThreadPriority = System.Threading.ThreadPriority;

// ReSharper disable ExtractCommonBranchingCode

namespace DevTools.Benchmarking;

/// <summary>
/// Crude benchmarking util for comparing expensive operations.
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

  private static void GetPartitionedArrays(int sampleSize, int partitions, out int[] thresholds, out long[] overhead,
    out long[] results)
  {
    thresholds = new int[partitions];
    for (int i = 0; i < partitions; i++)
      thresholds[i] = Mathf.CeilToInt(sampleSize * (float)(i + 1) / partitions);
    overhead = new long[partitions];
    results = new long[partitions];
  }

  private static (int sampleSize, int partitions) GetSampleSize(long ticks)
  {
    return Result.ToMicroseconds(ticks) switch
    {
      > 10_000 => (sampleSize: 10_000, partitions: 10),
      > 1_000  => (sampleSize: 100_000, partitions: 20),
      > 100    => (sampleSize: 250_000, partitions: 50),
      // Getting closer to Stopwatch granularity, more aggression on sample size to drown out overhead from Stopwatch.
      > 10 => (sampleSize: 10_000_000, partitions: 100),
      > 1  => (sampleSize: 100_000_000, partitions: 500),
      // Micro-benchmarking, requires strong amortization to get even remotely close to usable results. Anything less
      // than ~25% std. dev is statistically irrelevant and heavy noise here WILL blow that number out of the park.
      _ => (sampleSize: 500_000_000, partitions: 1_000)
    };
  }

  /// <returns>
  /// Time to execute <paramref name="function"/> 
  /// </returns>
  /// <param name="function">Function to execute each iteration.</param>
  /// <param name="measurement">Measurement of accuracy for benchmark results.</param>
  public static unsafe Result Run(delegate*<void> function, Measurement measurement = Measurement.Auto)
  {
    ShowWarnings();

    delegate*<void> noOp = &NoOp;

    // Elevate process and thread priority to minimize context switching
    using NormalizeScheduling ns = new();

    // Force JIT to compile
    function();
    noOp();

    (int sampleSize, int partitions) = EstimateSampleSize(function);
    GetPartitionedArrays(sampleSize, partitions, out int[] thresholds, out long[] overhead, out long[] results);

    // Do a pass right before we enter a no GC region
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();

    using (new LowerGcLatency(GCLatencyMode.LowLatency))
    {
      Measure(noOp, sampleSize, partitions, thresholds, overhead);
      Measure(function, sampleSize, partitions, thresholds, results);
    }

    for (int i = 0; i < partitions; i++)
      results[i] = Math.Max(0, results[i] - overhead[i]);

    return new Result(results, sampleSize, measurement);

    static (int sampleSize, int partitions) EstimateSampleSize(delegate*<void> function)
    {
      const int TestInterval = 5;
      const int EstimateIterations = 1000;

      // Ensure we're not jumping into expensive benchmark
      Stopwatch watch = Stopwatch.StartNew();
      for (int i = 0; i < TestInterval; i++)
        function();
      watch.Stop();

      // Max cutoff is 100ms per iteration. This is high enough that we could be measuring on individual calls
      if (watch.ElapsedMilliseconds > 100 * TestInterval)
      {
        Log.Warning(
          "Running benchmark on function that takes a long time to execute. To keep results reliable, iterations cannot be lowered further.");
        return (sampleSize: 100, partitions: 10);
      }

      // Estimate
      watch.Restart();
      for (int i = 0; i < EstimateIterations; i++)
        function();
      watch.Stop();
      return GetSampleSize(watch.ElapsedTicks);
    }

    // Stub for function invocation and loop overhead
    static void NoOp()
    {
    }

    static void Measure(delegate*<void> function, int sampleSize, int partitions, int[] thresholds,
      long[] ticks)
    {
      const int UnrollIncrement = 10;

      int batches = sampleSize / UnrollIncrement;
      int sampleIdx = 0;
      int callCount = 0;

      long previous = 0;
      int remainder = sampleSize;

      Stopwatch watch = Stopwatch.StartNew();
      if (batches > 0)
      {
        // Unroll loop for longer tests
        for (int i = 0; i < batches; i++, remainder -= UnrollIncrement)
        {
          function();
          function();
          function();
          function();
          function();
          function();
          function();
          function();
          function();
          function();

          callCount += 10;

          while (sampleIdx < partitions && callCount >= thresholds[sampleIdx])
          {
            long current = watch.ElapsedTicks;
            ticks[sampleIdx++] = current - previous;
            previous = current;
          }
        }
      }
      for (int i = remainder; --i >= 0;)
      {
        function();
        callCount++;

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

  /// <returns>
  /// Time to execute <paramref name="function"/> 
  /// </returns>
  /// <param name="function">Function to execute each iteration.</param>
  /// <param name="context">Object passed in with each function call.</param>
  /// <param name="measurement">Measurement of accuracy for benchmark results.</param>
  public static unsafe Result Run<T>(delegate*<ref T, void> function, T context,
    Measurement measurement = Measurement.Auto) where T : struct
  {
    ShowWarnings();

    delegate*<ref T, void> noOp = &NoOp;

    // Elevate process and thread priority to minimize context switching
    using NormalizeScheduling ns = new();

    // Force JIT to compile
    function(ref context);
    noOp(ref context);

    (int sampleSize, int partitions) = EstimateSampleSize(function, ref context);
    GetPartitionedArrays(sampleSize, partitions, out int[] thresholds, out long[] overhead, out long[] results);

    // Do a pass right before we start measuring
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();

    Measure(noOp, ref context, sampleSize, partitions, thresholds, overhead);
    Measure(function, ref context, sampleSize, partitions, thresholds, results);

    for (int i = 0; i < partitions; i++)
      results[i] = Math.Max(0, results[i] - overhead[i]);

    return new Result(results, sampleSize, measurement);

    static (int sampleSize, int partitions) EstimateSampleSize(delegate*<ref T, void> function, ref T context)
    {
      const int TestInterval = 5;
      const int EstimateIterations = 1000;

      // Ensure we're not jumping into expensive benchmark
      Stopwatch watch = Stopwatch.StartNew();
      for (int i = 0; i < TestInterval; i++)
        function(ref context);
      watch.Stop();

      // Max cutoff is 100ms per iteration. This is high enough that we could be measuring on individual calls
      if (watch.ElapsedMilliseconds > 100 * TestInterval)
      {
        Log.Warning(
          "Running benchmark on function that takes a long time to execute. To keep results reliable, iterations cannot be lowered further.");
        return (sampleSize: 100, partitions: 10);
      }

      // Estimate
      watch.Restart();
      for (int i = 0; i < EstimateIterations; i++)
        function(ref context);
      watch.Stop();
      return GetSampleSize(watch.ElapsedTicks);
    }

    // Stub for function invocation and loop overhead
    static void NoOp(ref T _)
    {
    }

    static void Measure(delegate*<ref T, void> function,
      ref T context, int sampleSize, int partitions, int[] thresholds, long[] ticks)
    {
      const int UnrollIncrement = 10;

      int batches = sampleSize / UnrollIncrement;
      int sampleIdx = 0;
      int callCount = 0;

      long previous = 0;
      int remainder = sampleSize;

      Stopwatch watch = Stopwatch.StartNew();
      if (batches > 0)
      {
        // Unroll loop for longer tests
        for (int i = 0; i < batches; i++, remainder -= UnrollIncrement)
        {
          function(ref context);
          function(ref context);
          function(ref context);
          function(ref context);
          function(ref context);
          function(ref context);
          function(ref context);
          function(ref context);
          function(ref context);
          function(ref context);

          callCount += 10;

          while (sampleIdx < partitions && callCount >= thresholds[sampleIdx])
          {
            long current = watch.ElapsedTicks;
            ticks[sampleIdx++] = current - previous;
            previous = current;
          }
        }
      }
      for (int i = remainder; --i >= 0;)
      {
        function(ref context);
        callCount++;

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

  /// <returns>
  /// Time to execute <paramref name="function"/> 
  /// </returns>
  /// <remarks>
  /// Uses delegate for benchmarking non-static functions or functions that use closure. This implementation will
  /// have lower accuracy due to the overhead of indirection and closure.
  /// </remarks>
  /// <param name="function">Function to execute each iteration.</param>
  /// <param name="measurement">Measurement of accuracy for benchmark results.</param>
  public static Result Run(Action function, Measurement measurement = Measurement.Auto)
  {
    ShowWarnings();

    // Elevate process and thread priority to minimize context switching
    using NormalizeScheduling ns = new();

    // Force JIT to compile
    function();
    NoOp();

    (int sampleSize, int partitions) = EstimateSampleSize(function);
    GetPartitionedArrays(sampleSize, partitions, out int[] thresholds, out long[] overhead, out long[] results);

    // Do a pass right before we enter a no GC region
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();

    using (new LowerGcLatency(GCLatencyMode.LowLatency))
    {
      Measure(NoOp, sampleSize, partitions, thresholds, overhead);
      Measure(function, sampleSize, partitions, thresholds, results);
    }

    long medianOverhead = overhead.MedianLong();
    for (int i = 0; i < partitions; i++)
      results[i] = Math.Max(0, results[i] - medianOverhead);

    return new Result(results, sampleSize, measurement);

    static (int sampleSize, int partitions) EstimateSampleSize(Action function)
    {
      const int TestInterval = 5;
      const int EstimateIterations = 1000;

      // Ensure we're not jumping into expensive benchmark
      Stopwatch watch = Stopwatch.StartNew();
      for (int i = 0; i < TestInterval; i++)
        function();
      watch.Stop();

      // Max cutoff is 100ms per iteration. This is high enough that we could be measuring on individual calls
      if (watch.ElapsedMilliseconds > 100 * TestInterval)
      {
        Log.Warning(
          "Running benchmark on function that takes a long time to execute. To keep results reliable, iterations cannot be lowered further.");
        return (sampleSize: 100, partitions: 10);
      }

      // Estimate
      watch.Restart();
      for (int i = 0; i < EstimateIterations; i++)
        function();
      watch.Stop();
      return GetSampleSize(watch.ElapsedTicks);
    }

    // Stub for function invocation and loop overhead
    static void NoOp()
    {
    }

    static void Measure(Action function, int sampleSize, int partitions, int[] thresholds,
      long[] ticks)
    {
      const int UnrollIncrement = 10;

      int batches = sampleSize / UnrollIncrement;
      int sampleIdx = 0;
      int callCount = 0;

      long previous = 0;
      int remainder = sampleSize;

      Stopwatch watch = Stopwatch.StartNew();
      if (batches > 0)
      {
        // Unroll loop for longer tests
        for (int i = 0; i < batches; i++, remainder -= UnrollIncrement)
        {
          function();
          function();
          function();
          function();
          function();
          function();
          function();
          function();
          function();
          function();

          callCount += 10;

          while (sampleIdx < partitions && callCount >= thresholds[sampleIdx])
          {
            long current = watch.ElapsedTicks;
            ticks[sampleIdx++] = current - previous;
            previous = current;
          }
        }
      }
      for (int i = remainder; --i >= 0;)
      {
        function();
        callCount++;

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

  [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
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
    private readonly int sample;

    private readonly double total;
    private readonly double mean;
    private readonly double median;
    private readonly double stdDev;

    public Result(long[] ticks, int sample,
      Measurement measurement = Measurement.Auto,
      int decimalPlaces = 4)
    {
      this.decimalPlaces = decimalPlaces;
      this.sample = sample;

      total = ticks.Sum();

      int callsPerPartition = sample / ticks.Length;
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

    [UsedImplicitly]
    public double Total => total;

    [UsedImplicitly]
    public double Mean => mean;

    [UsedImplicitly]
    public double Median => median;

    [UsedImplicitly]
    public double StdDev => stdDev;

    public string Formatted(double value)
    {
      return
        $"{value.ToString($"0.{new string('0', decimalPlaces)}")} {MeasurementSuffix(measurement)}";
    }

    public override string ToString()
    {
      return
        $"Sample={sample} | Total={Formatted(Total)} | Mean={Formatted(Mean)}";
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

  private readonly struct LowerGcLatency : IDisposable
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

  private readonly struct NormalizeScheduling : IDisposable
  {
    private readonly ProcessPriorityClass processPriority;
    private readonly ThreadPriority threadPriority;

    public NormalizeScheduling()
    {
      processPriority = Process.GetCurrentProcess().PriorityClass;
      threadPriority = Thread.CurrentThread.Priority;

      // We want to reduce as much as possible the chance of there being a context switch mid-execution
      // There's no way to completely eliminate this possibility but we can at least signal to the OS
      // this is time critical code.
      Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
      Thread.CurrentThread.Priority = ThreadPriority.Highest;
    }

    void IDisposable.Dispose()
    {
      Process.GetCurrentProcess().PriorityClass = processPriority;
      Thread.CurrentThread.Priority = threadPriority;
    }
  }

  // TODO
  //private readonly struct ThreadPin : IDisposable
  //{
  //  private readonly IntPtr prevAffinity;

  //  public ThreadPin(int core)
  //  {
  //    Process process = Process.GetCurrentProcess();
  //    prevAffinity = process.ProcessorAffinity;
  //    process.ProcessorAffinity = new IntPtr(1 << core);

  //    Thread.BeginThreadAffinity();
  //  }

  //  void IDisposable.Dispose()
  //  {
  //    throw new NotImplementedException();
  //  }
  //}
}