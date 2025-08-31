using System;

namespace DevTools.Testing;

public class NamespaceExpression : Expression
{
  public override Result CompareCase(ITestCase testCase, Comparison comparison, string value)
  {
    bool result = comparison switch
    {
      Comparison.Equals    => IsNamespace(testCase.Type, value),
      Comparison.NotEquals => !IsNamespace(testCase.Type, value),
      Comparison.Matches   => MatchesNamespace(testCase.Type, value),
      Comparison.NoMatches => !MatchesNamespace(testCase.Type, value),
      _                    => throw new NotImplementedException(),
    };
    return ToResult(result);

    static bool IsNamespace(Type type, string value)
    {
      return type.Namespace == value;
    }

    static bool MatchesNamespace(Type type, string value)
    {
      return type.Namespace != null && type.Namespace.Contains(value);
    }
  }
}