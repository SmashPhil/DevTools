using System;

namespace DevTools;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public abstract class MetaDataAttribute<T>(string key, T value) : Attribute, IMetaData
{
  public string Key { get; } = key;

  public object Value { get; } = value;
}