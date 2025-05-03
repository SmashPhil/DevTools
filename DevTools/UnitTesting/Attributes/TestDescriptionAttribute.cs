using System;

namespace DevTools.UnitTesting;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class TestDescriptionAttribute : TestTraitAttribute
{
  public TestDescriptionAttribute(string description) : base(MetaDataName.Description, description)
  {
  }
}