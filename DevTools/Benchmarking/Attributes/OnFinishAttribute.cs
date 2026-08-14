using System;
using JetBrains.Annotations;

namespace DevTools.Benchmarking;

/// <summary>
/// Marks a method to run after all benchmarks in the group have finished.
/// </summary>
[PublicAPI]
[AttributeUsage(AttributeTargets.Method)]
public class OnFinishAttribute : Attribute
{
}
