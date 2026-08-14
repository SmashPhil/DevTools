using System;
using JetBrains.Annotations;

namespace DevTools.Benchmarking;

/// <summary>
/// Marks a static field or property as the context supplied to benchmark methods.
/// </summary>
/// <remarks>A default context is used when no compatible context member is specified.</remarks>
[PublicAPI]
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class ContextAttribute : Attribute
{
}
