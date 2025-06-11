using System;

namespace DevTools;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public abstract class MetaDataAttribute : Attribute, IMetaData
{
  protected MetaDataAttribute(int key, object value)
  {
    this.Key = key;
    this.Value = value;
  }

  public int Key { get; }

  public object Value { get; }
}