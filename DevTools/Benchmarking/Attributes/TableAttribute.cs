using System;
using JetBrains.Annotations;

namespace DevTools.Benchmarking;

/// <summary>
/// Statistics available for benchmark result tables.
/// </summary>
[Flags]
public enum Stat
{
  /// <summary>
  /// Does not display benchmark statistics in a table.
  /// </summary>
  None = 0,

  /// <summary>
  /// Total execution time across all samples.
  /// </summary>
  Total = 1 << 0,

  /// <summary>
  /// Mean execution time per sample.
  /// </summary>
  Mean = 1 << 1,

  /// <summary>
  /// Median execution time per sample.
  /// </summary>
  Median = 1 << 2,

  /// <summary>
  /// Standard deviation of execution time per sample.
  /// </summary>
  StdDev = 1 << 3,

  /// <summary>
  /// Number of samples collected.
  /// </summary>
  Samples = 1 << 4,

  /// <summary>
  /// Number of sample partitions measured.
  /// </summary>
  Partitions = 1 << 5
}

/// <summary>
/// Specifies the statistics displayed in the benchmark results table.
/// </summary>
[PublicAPI]
[AttributeUsage(AttributeTargets.Class)]
public class TableAttribute : MetaDataAttribute<Stat>
{
  /// <param name="stats">Statistics displayed in the benchmark results table.</param>
  public TableAttribute(Stat stats) : base(MetaDataName.Table, stats)
  {
  }
}
