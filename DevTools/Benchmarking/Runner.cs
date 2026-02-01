using System;
using System.Diagnostics;
using System.Text;
using UnityEngine;
using Verse;

namespace DevTools.Benchmarking;

internal sealed class Runner : IRunner
{
  public const int TestInterval = 5;
  public const int EstimateIterations = 1000;
  public const int Iterations = EstimateIterations * TestInterval;

  private readonly Function testFunc;
  private readonly Function noOpFunc;

  public struct Function
  {
    public IntPtr funcPtr;
    public TestHarness harness;
  }

  private struct Ticks(int partitions)
  {
    public readonly long[] actual = new long[partitions];
    public readonly long[] overhead = new long[partitions];
    public readonly long[] deltas = new long[partitions];
  }

  public Runner(in Function testFunc, in Function noOpFunc)
  {
    this.testFunc = testFunc;
    this.noOpFunc = noOpFunc;
    
    UnrollFactor = testFunc.harness.UnrollFactor;
  }

  public Benchmark.Measurement Measurement { get; internal set; } = Benchmark.Measurement.Auto;

  public int Partitions => 20;

  private int UnrollFactor { get; }

  public Benchmark.Result Execute()
  {
#if DEBUG
    if (testFunc.harness.UnrollFactor != noOpFunc.harness.UnrollFactor)
    {
      Log.Error("Trying to execute benchmark with mismatched unroll factors.");
      return default;
    }
#endif

    // Elevate process and thread priority
    using NormalizeScheduling ns = new();
    int batches = GetBatchCount();
    Ticks ticks = new(Partitions);

    // Warmup
    for (int i = 0; i < IRunner.WarmUpIterations; i++)
    {
      for (int j = 0; j < batches; j++)
      {
        testFunc.harness.Invoke();
        noOpFunc.harness.Invoke();
      }
    }

    if (Prefs.LogVerbose)
    {
      StringBuilder report = new();
      report.AppendLine("---- Running benchmark ----");
      report.AppendLine($"Batches: {batches}");
      report.AppendLine($"Partitions: {Partitions}");
      report.AppendLine($"Invocations Per Batch: {UnrollFactor}");
      Log.Message(report.ToString());
    }

    // Do a pass right before we start measuring
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();

    Measure(batches, Partitions, ticks);

    if (Prefs.LogVerbose)
    {
      StringBuilder report = new();
      report.AppendLine("---- Result ----");
      report.AppendLine($"Actual: {string.Join(",", ticks.actual)}");
      report.AppendLine($"Overhead: {string.Join(",", ticks.overhead)}");
      report.AppendLine($"Delta: {string.Join(",", ticks.deltas)}");
      Log.Message(report.ToString());
    }

    return new Benchmark.Result(ticks.deltas, batches * UnrollFactor, Measurement);
  }

  private int GetBatchCount()
  {
    TestHarness harness = testFunc.harness;
    harness.Invoke(); // warmup

    // Make sure we're not jumping into an expensive estimation, clamp sample size if so
    Stopwatch watch = Stopwatch.StartNew();
    harness.Invoke();
    watch.Stop();

    // Estimate good sample size
    int batches = 0;
    long timestamp = Stopwatch.GetTimestamp();
    do
    {
      harness.Invoke();
      batches++;
    } while (HighPerfCounter.ElapsedMilliseconds(timestamp) < IRunner.TargetMsPerBatch);

    return batches;
  }

  private void Measure(int batches, int partitions, in Ticks ticks)
  {
    TestHarness test = testFunc.harness;
    TestHarness noOp = noOpFunc.harness;

    for (int i = 0; i < partitions; i++)
    {
      long actual, overhead;
      if ((i & 1) == 0)
      {
        actual = RunPartition(test, batches);
        overhead = RunPartition(noOp, batches);
      }
      else
      {
        overhead = RunPartition(noOp, batches);
        actual = RunPartition(test, batches);
      }

      ticks.actual[i] = actual;
      ticks.overhead[i] = overhead;
      ticks.deltas[i] = actual - overhead;
    }
    return;

    static long RunPartition(TestHarness harness, int batches)
    {
      // Actual
      long t0 = Stopwatch.GetTimestamp();
      for (int j = 0; j < batches; j++)
      {
        harness.Invoke();
      }
      long t1 = Stopwatch.GetTimestamp();

      return t1 - t0;
    }
  }
}