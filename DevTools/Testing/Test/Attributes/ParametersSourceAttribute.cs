using System;
using JetBrains.Annotations;

namespace DevTools.Testing;

/// <summary>
/// Provides values from a named source as parameter values.
/// </summary>
[PublicAPI]
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class ParametersSourceAttribute : Attribute
{
  /// <summary>
  /// Source for parameter.
  /// </summary>
  /// <param name="name">
  /// Name of the field or getter method.
  /// Must be defined within the declaring class of this method.
  /// </param>
  public ParametersSourceAttribute(string name)
  {
    Name = name;
  }

  /// <summary>
  /// Source for parameter.
  /// </summary>
  /// <param name="type">Declaring type of the source.</param>
  /// <param name="name">Name of the field or getter method.</param>
  public ParametersSourceAttribute(Type type, string name)
  {
    Type = type;
    Name = name;
  }

  /// <summary>
  /// Declaring type of the source.
  /// </summary>
  public Type Type { get; }

  /// <summary>
  /// Name of the field or getter method.
  /// </summary>
  public string Name { get; }
}
