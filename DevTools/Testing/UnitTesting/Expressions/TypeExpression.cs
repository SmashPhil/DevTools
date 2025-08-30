using System;

namespace DevTools.UnitTesting;

public class TypeExpression : Expression
{
  public override Result CompareCase(ITestCase testCase, Comparison comparison, string value)
  {
    bool result = comparison switch
    {
      Comparison.Equals    => IsType(testCase.Type, value),
      Comparison.NotEquals => !IsType(testCase.Type, value),
      Comparison.Matches   => Matches(testCase.Type, value),
      Comparison.NoMatches => !Matches(testCase.Type, value),
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