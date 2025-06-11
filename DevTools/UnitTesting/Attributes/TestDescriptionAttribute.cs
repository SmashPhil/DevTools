using System;

namespace DevTools.UnitTesting;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class TestDescriptionAttribute : MetaDataAttribute
{
  public TestDescriptionAttribute(string description) : base(MetaDataName.Description, description)
  {
  }
}