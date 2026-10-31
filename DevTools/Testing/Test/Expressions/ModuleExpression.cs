using System;

namespace DevTools.Testing;

public class ModuleExpression : Expression
{
  public override Result CompareFixture(ITestFixture fixture, Comparison comparison, string value)
  {
    bool result = comparison switch
    {
      Comparison.Equals => IsModule(fixture.Module, value),
      Comparison.NotEquals => !IsModule(fixture.Module, value),
      Comparison.Matches => MatchesModule(fixture.Module, value),
      Comparison.NoMatches => !MatchesModule(fixture.Module, value),
      _ => throw new NotImplementedException(),
    };
    return ToResult(result);

    static bool IsModule(ITestModule module, string value)
    {
      return module.Name == value;
    }

    static bool MatchesModule(ITestModule module, string value)
    {
      return module is { Name: not null } && module.Name.Contains(value);
    }
  }
}
