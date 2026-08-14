using System;
using JetBrains.Annotations;

namespace DevTools.Benchmarking;

/// <summary>
/// Marks a method to run before any benchmarks in the group begin.
/// </summary>
[PublicAPI]
[AttributeUsage(AttributeTargets.Method)]
public class PrepareAttribute : Attribute
{
}
