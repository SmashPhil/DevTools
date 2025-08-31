using System;

namespace DevTools.Testing;

public class TestTypeExpression : Expression
{
  public override Result CompareGroup(ITestGroup group, Comparison comparison, string value)
  {
    bool result = comparison switch
    {
      Comparison.Equals    => IsTestType(group.TestType, value),
      Comparison.NotEquals => !IsTestType(group.TestType, value),
      Comparison.Matches   => MatchesTestType(group.TestType, value),
      Comparison.NoMatches => !MatchesTestType(group.TestType, value),
      _                    => throw new NotImplementedException(),
    };
    return ToResult(result);

    static bool IsTestType(TestType testType, string value)
    {
      return testType.ToString() == value;
    }

    static bool MatchesTestType(TestType testType, string value)
    {
      return testType.ToString().Contains(value);
    }
  }
}