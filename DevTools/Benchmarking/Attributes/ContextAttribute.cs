using System;

namespace DevTools.Benchmarking;

/// <summary>
/// Marks a field or property as the context object for the benchmark class.
/// </summary>
/// <remarks>Use for specifying a specific context object, otherwise a default object will be initialized.</remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class ContextAttribute : Attribute
{
}