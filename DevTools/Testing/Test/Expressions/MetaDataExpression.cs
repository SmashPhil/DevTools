using System;

namespace DevTools.Testing;

public class MetaDataExpression : Expression
{
  private readonly string metaDataKey;

  public MetaDataExpression(string key)
  {
    metaDataKey = key;
  }

  public override Result CompareCase(ITestCase testCase, Comparison comparison, string value)
  {
    object metaDataObj = testCase.MetaData.GetRaw(metaDataKey);
    if (metaDataObj == null)
      return Result.False;

    string otherValue = metaDataObj.ToString();

    bool result = comparison switch
    {
      Comparison.Equals    => otherValue == value,
      Comparison.NotEquals => otherValue != value,
      Comparison.Matches   => otherValue.Contains(value),
      Comparison.NoMatches => !otherValue.Contains(value),
      _                    => throw new NotImplementedException(),
    };
    return ToResult(result);
  }
}