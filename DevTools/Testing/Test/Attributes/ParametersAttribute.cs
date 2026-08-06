using System;
using JetBrains.Annotations;

namespace DevTools.Testing;

/// <summary>
/// Provides inline parameter values.
/// </summary>
[PublicAPI]
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class ParametersAttribute(params object[] args) : Attribute
{
  /// <summary>
  /// Parameter values.
  /// </summary>
  public object[] Arguments { get; } = args;
}
