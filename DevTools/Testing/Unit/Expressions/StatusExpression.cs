using System;

namespace DevTools.Testing;

public class StatusExpression : Expression
{
  public override Result CompareCase(ITestCase testCase, Comparison comparison, string value)
  {
    bool result = comparison switch
    {
      Comparison.Equals or Comparison.Matches      => IsStatus(testCase.Status, value),
      Comparison.NotEquals or Comparison.NoMatches => !IsStatus(testCase.Status, value),
      _                                            => throw new NotImplementedException(),
    };
    return ToResult(result);

    static bool IsStatus(Status status, string value)
    {
      return string.Equals(status.ToString(), value, StringComparison.InvariantCultureIgnoreCase);
    }
  }
}