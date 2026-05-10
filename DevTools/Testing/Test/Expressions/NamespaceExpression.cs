using System;

namespace DevTools.Testing;

public class NamespaceExpression : Expression
{
  public override Result CompareFixture(ITestFixture fixture, Comparison comparison, string value)
  {
    bool result = comparison switch
    {
      Comparison.Equals => IsNamespace(fixture.Type, value),
      Comparison.NotEquals => !IsNamespace(fixture.Type, value),
      Comparison.Matches => MatchesNamespace(fixture.Type, value),
      Comparison.NoMatches => !MatchesNamespace(fixture.Type, value),
      _ => throw new NotImplementedException(),
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