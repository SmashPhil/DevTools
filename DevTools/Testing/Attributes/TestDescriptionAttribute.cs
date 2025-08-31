using System;

namespace DevTools.Testing;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class TestDescriptionAttribute : MetaDataAttribute<string>
{
  public TestDescriptionAttribute(string description) : base(MetaDataName.Description, description)
  {
  }
}