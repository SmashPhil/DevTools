using System;
using System.Collections.Generic;

namespace DevTools.Testing;

internal sealed class ExpressionFilter
{
  private readonly Node root;

  public ExpressionFilter(string expression)
  {
    if (string.IsNullOrWhiteSpace(expression))
      throw new ArgumentException("Filter expression cannot be empty.", nameof(expression));

    root = CreateParser().Evaluate(expression);
  }

  public bool Filter(ITestFixture fixture, ITestFunction function)
  {
    ArgumentNullException.ThrowIfNull(fixture);
    ArgumentNullException.ThrowIfNull(function);
    return root.Evaluate(fixture, function);
  }

  private static ShuntingYard<OperatorType, Node> CreateParser()
  {
    ShuntingYard<OperatorType, Node> parser = new(
      static token => new TermNode(token),
      evalBinary: CreateNode);

    // There are some duplicate symbols to cover dotnet test compatibility
    // e.g. '==' and '=' mean the same thing, but only '=' is supported by dotnet test
    parser.AddBinaryOperator("=", OperatorType.Equals, precedence: 3);
    parser.AddBinaryOperator("==", OperatorType.Equals, precedence: 3);
    parser.AddBinaryOperator("!=", OperatorType.NotEquals, precedence: 3);
    parser.AddBinaryOperator("~", OperatorType.Contains, precedence: 3);
    parser.AddBinaryOperator("=~", OperatorType.Contains, precedence: 3);
    parser.AddBinaryOperator("!~", OperatorType.Excludes, precedence: 3);
    parser.AddBinaryOperator("&", OperatorType.And, precedence: 2);
    parser.AddBinaryOperator("&&", OperatorType.And, precedence: 2);
    parser.AddBinaryOperator("|", OperatorType.Or, precedence: 1);
    parser.AddBinaryOperator("||", OperatorType.Or, precedence: 1);
    return parser;
  }

  private static Node CreateNode(Node lhs, Node rhs, OperatorType type)
  {
    if (type is OperatorType.And or OperatorType.Or)
      return new LogicalNode(lhs, rhs, type);

    if (lhs is not TermNode property || rhs is not TermNode value)
      throw new ArgumentException("A comparison must have a property and a value.");

    return new ComparisonNode(property.Value, value.Value, type);
  }

  private abstract class Node
  {
    public abstract bool Evaluate(ITestFixture fixture, ITestFunction function);
  }

  private sealed class TermNode(string value) : Node
  {
    public string Value => value;

    public override bool Evaluate(ITestFixture fixture, ITestFunction function)
    {
      return function.FullName != null &&
        function.FullName.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
    }
  }

  private sealed class ComparisonNode(string property, string value, OperatorType type) : Node
  {
    public override bool Evaluate(ITestFixture fixture, ITestFunction function)
    {
      bool matched = false;
      foreach (string candidate in GetValues(property, fixture, function))
      {
        if (candidate == null)
          continue;

        if (type is OperatorType.Equals or OperatorType.NotEquals
          ? string.Equals(candidate, value, StringComparison.OrdinalIgnoreCase)
          : candidate.Contains(value, StringComparison.OrdinalIgnoreCase))
        {
          matched = true;
          break;
        }
      }

      return type is OperatorType.NotEquals or OperatorType.Excludes ? !matched : matched;
    }
  }

  private sealed class LogicalNode(Node lhs, Node rhs, OperatorType type) : Node
  {
    public override bool Evaluate(ITestFixture fixture, ITestFunction function)
    {
      return type == OperatorType.And
        ? lhs.Evaluate(fixture, function) && rhs.Evaluate(fixture, function)
        : lhs.Evaluate(fixture, function) || rhs.Evaluate(fixture, function);
    }
  }

  private static IEnumerable<string> GetValues(string property, ITestFixture fixture,
    ITestFunction function)
  {
    switch (property.ToLowerInvariant())
    {
      case "category":
        foreach (string category in fixture.MetaData.Get<string[]>(MetaDataName.Category) ?? [])
          yield return category;
        foreach (string category in function.MetaData.Get<string[]>(MetaDataName.Category) ?? [])
          yield return category;
        break;
      case "class":
        yield return fixture.Type.Name;
        yield return fixture.Type.FullName;
        yield return fixture.Type.AssemblyQualifiedName;
        break;
      case "fullyqualifiedname":
        yield return function.FullName;
        break;
      case "method":
        yield return function.MethodInfo.Name;
        if (function.MethodInfo.DeclaringType is { } declaringType)
        {
          yield return $"{declaringType.Name}.{function.MethodInfo.Name}";
          yield return $"{declaringType.FullName}.{function.MethodInfo.Name}";
        }
        break;
      case "module":
        yield return fixture.Module?.Name;
        break;
      case "name":
        yield return function.Name;
        break;
      case "namespace":
        yield return fixture.Type.Namespace;
        break;
      case "status":
        yield return fixture.Status.ToString();
        yield return function.Status.ToString();
        break;
      case "type":
        yield return fixture.TestType.ToString();
        break;
      default:
        yield return fixture.MetaData.GetRaw(property) as string;
        yield return function.MetaData.GetRaw(property) as string;
        break;
    }
  }

  private enum OperatorType
  {
    And,
    Or,
    Equals,
    NotEquals,
    Contains,
    Excludes
  }
}
