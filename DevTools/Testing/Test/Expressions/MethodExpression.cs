using System;

namespace DevTools.Testing;

public class MethodExpression : Expression
{
  public override Result CompareFunction(ITestFunction testFunction, Comparison comparison,
    string value)
  {
    bool result = comparison switch
    {
      Comparison.Equals    => IsMethod(testFunction, value),
      Comparison.NotEquals => !IsMethod(testFunction, value),
      Comparison.Matches   => MatchesMethod(testFunction, value),
      Comparison.NoMatches => !MatchesMethod(testFunction, value),
      _                    => throw new NotImplementedException(),
    };
    return ToResult(result);

    static bool IsMethod(ITestFunction testFunction, string value)
    {
      if (testFunction.MethodInfo.DeclaringType is null)
        throw new NotSupportedException("Global test functions are not supported");
      return testFunction.MethodInfo.Name == value ||
        $"{testFunction.MethodInfo.DeclaringType.Name}.{testFunction.MethodInfo.Name}" == value ||
        $"{testFunction.MethodInfo.DeclaringType.FullName}.{testFunction.MethodInfo.Name}" == value;
    }

    static bool MatchesMethod(ITestFunction testFunction, string value)
    {
      if (testFunction.MethodInfo.DeclaringType is null)
        throw new NotSupportedException("Global test functions are not supported");
      return testFunction.MethodInfo.Name.Contains(value) ||
        $"{testFunction.MethodInfo.DeclaringType.Name}.{testFunction.MethodInfo.Name}"
         .Contains(value) ||
        $"{testFunction.MethodInfo.DeclaringType.FullName}.{testFunction.MethodInfo.Name}"
         .Contains(value);
    }
  }
}