using System;

namespace DevTools.UnitTesting;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public abstract class TestTraitAttribute : Attribute, IMetaData
{
  protected TestTraitAttribute(int key, string value)
  {
    this.Key = key;
    this.Value = value;
  }

  public int Key { get; }

  public object Value { get; }
}