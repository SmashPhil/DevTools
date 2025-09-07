using System;
using JetBrains.Annotations;
using LudeonTK;

namespace DevTools.Benchmarking;

/// <summary>
/// Marks a class as a benchmark container discoverable by the benchmark manager.
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
	/// Logical group name for the benchmark group.
	/// </summary>
	/// <remarks>Used for grouping benchmarks in the DevTools menu.</remarks>
	public string Category { get; }

	/// <summary>
	/// When <see langword="true"/>, the runner will execute benchmarks for this class from a long event.
	/// Set to <see langword="false"/> for main-thread/synchronous execution.
	/// </summary>
	public bool RunAsync { get; set; } = true;

	/// <summary>
	/// Game states in which these benchmarks are allowed to run.
	/// </summary>
	public AllowedGameStates AllowedGameStates { get; set; }
}