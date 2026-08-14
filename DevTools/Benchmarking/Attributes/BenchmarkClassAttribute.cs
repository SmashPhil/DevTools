using System;
using JetBrains.Annotations;
using LudeonTK;

namespace DevTools.Benchmarking;

/// <summary>
/// Marks a class containing benchmark methods.
/// </summary>
[PublicAPI, AttributeUsage(AttributeTargets.Class)]
public class BenchmarkClassAttribute : Attribute
{
  public BenchmarkClassAttribute()
  {
  }

  /// <param name="category">Logical group name for the benchmark group.</param>
  public BenchmarkClassAttribute(string category)
  {
    Category = category;
  }

  /// <summary>
  /// Name used to group benchmarks in the DevTools menu.
  /// </summary>
  public string Category { get; }

  /// <summary>
  /// Whether benchmarks in this class can run off the main thread.
  /// </summary>
  public bool RunAsync { get; set; } = true;

  /// <summary>
  /// Game states in which these benchmarks are allowed to run.
  /// </summary>
  public AllowedGameStates AllowedGameStates { get; set; }
}
