namespace DevTools.Testing;

internal class MessageBuilder
{
  public static string BooleanFailureMessage(bool value)
  {
    return $"Value was {value}";
  }

  public static string ComparisonFailureMessage<T>(T lhs, T rhs)
  {
    return $"Operand1={lhs}, Operand2={rhs}";
  }
}