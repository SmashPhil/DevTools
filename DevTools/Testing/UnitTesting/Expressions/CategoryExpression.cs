using System;
using Verse;

namespace DevTools.UnitTesting;

public class CategoryExpression : Expression
{
  private readonly int metaDataKey;

  public CategoryExpression(int metaDataKey)
  {
    this.metaDataKey = metaDataKey;
  }

  public CategoryExpression(string key)
  {
    this.metaDataKey = key.GetHashCode();
  }

  public override Result CompareCase(ITestCase testCase, Comparison comparison, string value)
  {
    string[] testCategories = testCase.MetaData.Get<string[]>(metaDataKey);
    if (testCategories.NullOrEmpty())
      return Result.False;

    foreach (string category in testCategories)
    {
      bool result = comparison switch
      {
        Comparison.Equals    => category == value,
        Comparison.NotEquals => category != value,
        Comparison.Matches   => category.Contains(value),
        Comparison.NoMatches => !category.Contains(value),
        _                    => throw new NotImplementedException(),
      };
      if (result)
        return Result.True;
    }
    return Result.False;
  }
}