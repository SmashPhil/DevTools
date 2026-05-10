using System;
using Verse;

namespace DevTools.Testing;

public class CategoryExpression : Expression
{
  private readonly string metaDataKey;

  public CategoryExpression(string key)
  {
    metaDataKey = key;
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