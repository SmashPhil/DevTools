using System;
using JetBrains.Annotations;

namespace DevTools.Testing;

[PublicAPI]
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class ParametersSourceAttribute : Attribute
{
  public ParametersSourceAttribute(string name)
  {
    FieldName = name;
  }

  public ParametersSourceAttribute(Type type, string name)
  {
    Type = type;
    FieldName = name;
  }

  public Type Type { get; }

  public string FieldName { get; }
}
