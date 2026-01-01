using System;

namespace DevTools.Testing;

internal class MessageBuilder
{
  private const string Expected = "Expected:";
  private const string AssertionFailed = "Assertion failure.";

  public static string GetMessage(string failureMessage)
  {
    return $"{AssertionFailed} {failureMessage}";
  }

  public static string GetMessage(string failureMessage, string expected)
  {
    return GetMessage($"{failureMessage}{Environment.NewLine}{Expected} {expected}");
  }

  public static string GetEqualityMessage(object actual, object expected, bool expectEqual)
  {
    string failureMessage = $"Values are {(expectEqual ? "not " : "")}equal.";
    return GetMessage(failureMessage, $"{actual} {(expectEqual ? "==" : "!=")} {expected}");
  }

  public static string NullFailureMessage(object value, bool expectNull)
  {
    string failureMessage = $"Value was {(expectNull ? "not " : "")}Null";
    return GetMessage(failureMessage, $"Value was {(expectNull ? "" : "not ")}Null");
  }

  public static string BooleanFailureMessage(bool expected)
  {
    return GetMessage($"Value was {!expected}", expected.ToString());
  }

  public static string GetComparisonMessage(object value, object operand, NumericalComparison comparison)
  {
    return GetMessage($"Value was not {GetCompString(comparison)} {operand}", $"{value} {GetCompString(comparison)} {operand}");

    static string GetCompString(NumericalComparison comparison)
    {
      return comparison switch
      {
        NumericalComparison.LessThan => "<",
        NumericalComparison.GreaterThan => ">",
        NumericalComparison.LessThanOrEqual => "<=",
        NumericalComparison.GreaterThanOrEqual => ">=",
        _ => throw new NotImplementedException(),
      };
    }
  }

  public enum NumericalComparison
  {
    LessThan,
    GreaterThan,
    LessThanOrEqual,
    GreaterThanOrEqual
  }
}