using System;

namespace DevTools.UnitTesting;

[AttributeUsage(AttributeTargets.Class)]
public class TestCategoryAttribute : TestTraitAttribute
{
  public TestCategoryAttribute(string category) : base(MetaDataName.Category, category)
  {
  }
}