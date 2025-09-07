using System;
using JetBrains.Annotations;

namespace DevTools.Benchmarking;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
[AttributeUsage(AttributeTargets.Method)]
public class BenchmarkAttribute : Attribute
{
	/// <summary>
	/// Name of this benchmark.
	/// </summary>
	/// <remarks>If empty, the method name will be used.</remarks>
	public string Label { get; set; }
}