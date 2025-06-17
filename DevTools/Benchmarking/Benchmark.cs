using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Threading;
using JetBrains.Annotations;
using UnityEngine;
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
      Verse.Log.WarningOnce(
        "Benchmarks should not be executed with debugger attached. The results will be wildly inaccurate.",
        "Benchmark.DebuggerAttached".GetHashCode());
    }
#endif
  }

  private static int GetPartitionedArrays(int sampleSize, out int[] thresholds, out long[] overhead,
    out long[] results)
  {
    int partitions = Mathf.Min(Mathf.Max(10, sampleSize / 1000), sampleSize);

    thresholds = new int[partitions];
    for (int i = 0; i < partitions; i++)
      thresholds[i] = Mathf.CeilToInt(sampleSize * (float)(i + 1) / partitions);
    overhead = new long[partitions];
    results = new long[partitions];
    return partitions;
  }

  /// <returns>
  /// Time to run N <paramref name="sampleSize"/> of <paramref name="function"/> 
  /// </returns>
  /// <param name="function">Function to execute each iteration.</param>
  /// <param name="sampleSize">Number of times to run this benchmark test.</param>
  /// <param name="measurement">Measurement of accuracy for benchmark results.</param>
  public static unsafe Result Run(delegate*<void> function, int sampleSize,
    Measurement measurement = Measurement.Auto)
  {
    ShowWarnings();

    delegate*<void> noOp = &NoOp;

    // Elevate process and thread priority to minimize context switching
    using NormalizeScheduling ns = new();

    // Force JIT to compile
    function();
    noOp();

    int partitions = GetPartitionedArrays(sampleSize, out int[] thresholds, out long[] overhead,
      out long[] results);

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
  /// Time to run N <paramref name="sampleSize"/> of <paramref name="function"/> 
  /// </returns>
  /// <param name="function">Function to execute each iteration.</param>
  /// <param name="context">Object passed in with each function call.</param>
  /// <param name="sampleSize">Number of times to run this benchmark test.</param>
  /// <param name="measurement">Measurement of accuracy for benchmark results.</param>
  public static unsafe Result Run<T>(delegate*<ref T, void> function, T context, int sampleSize,
    Measurement measurement = Measurement.Auto)
    where T : struct
  {
    ShowWarnings();

    delegate*<ref T, void> noOp = &NoOp;

    // Elevate process and thread priority to minimize context switching
    using NormalizeScheduling ns = new();

    // Force JIT to compile
    function(ref context);
    noOp(ref context);

    int partitions = GetPartitionedArrays(sampleSize, out int[] thresholds, out long[] overhead,
      out long[] results);

    // Do a pass right before we start measuring
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();

    Measure(noOp, ref context, sampleSize, partitions, thresholds, overhead);
    Measure(function, ref context, sampleSize, partitions, thresholds, results);

    for (int i = 0; i < partitions; i++)
      results[i] = Math.Max(0, results[i] - overhead[i]);

    return new Result(results, sampleSize, measurement);

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
  /// Time to run N <paramref name="sampleSize"/> of <paramref name="function"/> 
  /// </returns>
  /// <remarks>
  /// Uses delegate for benchmarking non-static functions or functions that need 
  /// to capture. This implementation will have lower accuracy due to the additional 
  /// cost of indirection and closures.
  /// </remarks>
  /// <param name="function">Function to execute each iteration.</param>
  /// /// <param name="sampleSize">Number of times to run this benchmark test.</param>
  /// <param name="measurement">Measurement of accuracy for benchmark results.</param>
  public static Result Run(Action function, int sampleSize,
    Measurement measurement = Measurement.Auto)
  {
    ShowWarnings();

    // Elevate process and thread priority to minimize context switching
    using NormalizeScheduling ns = new();

    // Force JIT to compile
    function();
    NoOp();

    int partitions = GetPartitionedArrays(sampleSize, out int[] thresholds, out long[] overhead,
      out long[] results);

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
      Measurement.Auto => throw new NotImplementedException(),
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
    private static readonly Measurement[] orderedMeasurements =
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
      foreach (Measurement curMeasurement in orderedMeasurements)
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
    private static double ToSeconds(double ticks)
    {
      return ticks / Stopwatch.Frequency;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double ToMilliseconds(double ticks)
    {
      return ticks * 1000 / Stopwatch.Frequency;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double ToMicroseconds(double ticks)
    {
      return ticks * 1_000_000 / Stopwatch.Frequency;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double ToNanoseconds(double ticks)
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