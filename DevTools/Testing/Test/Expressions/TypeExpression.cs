using System;

namespace DevTools.Testing;

public class TypeExpression : Expression
{
  public override Result CompareFixture(ITestFixture fixture, Comparison comparison, string value)
  {
    bool result = comparison switch
    {
      Comparison.Equals    => IsType(fixture.Type, value),
      Comparison.NotEquals => !IsType(fixture.Type, value),
      Comparison.Matches   => Matches(fixture.Type, value),
      Comparison.NoMatches => !Matches(fixture.Type, value),
      _                    => throw new NotImplementedException(),
    };
    return ToResult(result);

    static bool IsType(Type type, string value)
    {
      return type.AssemblyQualifiedName == value || type.Name == value || type.FullName == value;
    }

    static bool Matches(Type type, string value)
    {
      return (type.AssemblyQualifiedName != null && type.AssemblyQualifiedName.Contains(value)) ||
        (type.FullName != null && type.FullName.Contains(value)) ||
        type.Name.Contains(value);
    }
  }
}