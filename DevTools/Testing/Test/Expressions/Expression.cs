namespace DevTools.Testing;

public abstract class Expression
{
  public virtual Result CompareCase(ITestCase testCase, Comparison comparison, string value)
  {
    return Result.Undefined;
  }

  public virtual Result CompareFixture(ITestFixture fixture, Comparison comparison, string value)
  {
    return Result.Undefined;
  }

  public virtual Result CompareFunction(ITestFunction function, Comparison comparison,
    string value)
  {
    return Result.Undefined;
  }

  protected static Result ToResult(bool value)
  {
    return value ? Result.True : Result.False;
  }

  public enum Comparison
  {
    Equals,
    NotEquals,
    Matches,
    NoMatches
  }

  public enum Boolean
  {
    And,
    Or
  }

  public enum Result
  {
    Undefined = -1,
    False = 0,
    True = 1,
  }
}