using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine.Assertions;
using static DevTools.Testing.Expression;

namespace DevTools.Testing;

[PublicAPI]
public class ExpressionTree
{
  private Node root;
  private Expression.Boolean? nextBoolean;

  internal bool Filter(ITestFixture fixture, ITestFunction function)
  {
    return root?.Evaluate(fixture, function) ?? true;
  }

  public void Add(Expression expression, Comparison comparison, string value)
  {
    Node node = new ExpressionNode(expression, comparison, value);
    if (root is null)
    {
      root = node;
    }
    else if (nextBoolean.HasValue)
    {
      root = new LogicalNode(root, node, nextBoolean.Value);
      nextBoolean = null; // Consume boolean
    }
    else
    {
      throw new InvalidOperationException("Invalid expression");
    }
  }

  public void SetBoolean(Expression.Boolean boolean)
  {
    if (root is null)
      throw new ArgumentException("Cannot start expressions with logical operator.");
    nextBoolean = boolean;
  }

  public abstract class Node
  {
    public abstract bool Evaluate(ITestFixture group, ITestFunction function);
  }

  public class ExpressionNode : Node
  {
    private readonly Expression expression;
    private readonly Comparison comparison;
    private readonly string value;

    public ExpressionNode(Expression expression, Comparison comparison, string value)
    {
      this.expression = expression;
      this.comparison = comparison;
      this.value = value;
    }

    public override bool Evaluate(ITestFixture group, ITestFunction function)
    {
      Result groupCaseResult = expression.CompareCase(group, comparison, value);
      Result functionCaseResult = expression.CompareCase(function, comparison, value);

      Result groupResult = expression.CompareFixture(group, comparison, value);
      Result funcResult = expression.CompareFunction(function, comparison, value);

      Result result = Combine(Combine(Combine(groupCaseResult, functionCaseResult), groupResult),
        funcResult);
      Assert.IsFalse(result == Result.Undefined);
      return result == Result.True;

      static Result Combine(Result lhs, Result rhs)
      {
        if (lhs == Result.True || rhs == Result.True)
          return Result.True;
        if (lhs == Result.False || rhs == Result.False)
          return Result.False;
        return Result.Undefined;
      }
    }
  }

  public class LogicalNode : Node
  {
    private readonly Node left;
    private readonly Node right;
    private readonly Expression.Boolean boolean;

    public LogicalNode(Node left, Node right, Expression.Boolean boolean)
    {
      this.left = left;
      this.right = right;
      this.boolean = boolean;
    }

    public override bool Evaluate(ITestFixture group, ITestFunction function)
    {
      bool lhs = left.Evaluate(group, function);
      bool rhs = right.Evaluate(group, function);
      return boolean switch
      {
        Expression.Boolean.And => lhs && rhs,
        Expression.Boolean.Or => lhs || rhs,
        _ => throw new NotImplementedException(nameof(Expression.Boolean))
      };
    }
  }
}