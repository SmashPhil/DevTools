using System;

namespace DevTools.UnitTesting;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class TestCategoryAttribute : MetaDataAttribute
{
  public TestCategoryAttribute(string category) : base(MetaDataName.Category, category)
  {
  }

  public TestCategoryAttribute(params string[] categories) : base(MetaDataName.Category, categories)
  {
  }
}