using System;
using System.Text.RegularExpressions;
using static DevTools.UnitTesting.Expression;

namespace DevTools.UnitTesting;

internal static class ExpressionGenerator
{
  private const string ExpressionRegex = "\"[^\"]*\"|\\(|\\)|\\|\\||&&|==|!=|=~|!~|[\\w\\d_.\\-]+";

  private static Expression GetExpression(string key)
  {
    return key.ToLowerInvariant() switch
    {
      "category"  => new CategoryExpression(MetaDataName.Category),
      "method"    => new MethodExpression(),
      "class"     => new TypeExpression(),
      "namespace" => new NamespaceExpression(),
      "type"      => new TestTypeExpression(),
      "status"    => new StatusExpression(),
      // By default, if key cannot be found it will be checked for property based filter
      _ => new MetaDataExpression(key)
    };
  }

  private static Expression.Boolean GetBoolean(string key)
  {
    return key.ToLowerInvariant() switch
    {
      "&&" or "and" => Expression.Boolean.And,
      "||" or "or"  => Expression.Boolean.Or,
      _             => throw new ArgumentException(key)
    };
  }

  private static Comparison GetComparison(string key)
  {
    return key.ToLowerInvariant() switch
    {
      "==" or "equals"    => Comparison.Equals,
      "!=" or "notequals" => Comparison.NotEquals,
      "=~" or "matches"   => Comparison.Matches,
      "!~" or "nomatches" => Comparison.NoMatches,
      _                   => throw new ArgumentException(key)
    };
  }

  public static ExpressionTree Create(string expressionStr)
  {
    MatchCollection matches =
      Regex.Matches(expressionStr, ExpressionRegex, RegexOptions.IgnoreCase);

    ExpressionTree expressionTree = new();

    // Should be in blocks of 3, key operator value eg. trait == Patches
    for (int i = 0; i < matches.Count; i++)
    {
      Match match = matches[i];
      string key = match.Value;
      Expression expression = GetExpression(key);
      if (i + 2 >= matches.Count)
        throw new ArgumentException($"expression {expression} not formatted correctly.");
      string comparisonStr = matches[++i].Value;
      string valueStr = matches[++i].Value;
      Comparison comparison = GetComparison(comparisonStr);
      expressionTree.Add(expression, comparison, valueStr);

      if (i < matches.Count - 2)
      {
        // Prepare logical operator for next condition
        key = matches[++i].Value;
        Expression.Boolean boolean = GetBoolean(key);
        expressionTree.SetBoolean(boolean);
      }
    }
    return expressionTree;
  }
}