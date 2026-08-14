using System;
using JetBrains.Annotations;

namespace DevTools.Benchmarking;

/// <summary>
/// Marks a method as a benchmark.
/// </summary>
[PublicAPI]
[AttributeUsage(AttributeTargets.Method)]
public class BenchmarkAttribute : Attribute
{
	/// <summary>
	/// Name of this benchmark.
	/// </summary>
	/// <remarks>If empty, the method name will be used.</remarks>
	public string Label { get; set; }
}
