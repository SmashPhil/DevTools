using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine.Assertions;

namespace DevTools;

[Flags]
internal enum UnaryAffix { Prefix = 1, Postfix = 2 }

/// <summary>
/// Converts infix expressions into reverse Polish notation using the shunting yard algorithm,
/// then evaluates the expression.
/// </summary>
/// <typeparam name="TOperator">The operator token type used by the expression parser.</typeparam>
/// <typeparam name="TOperand">The operand token type used by the expression parser.</typeparam>
/// <remarks>
/// All operations are left-associative. If an operator should be right-associative, it must be grouped
/// with parenthesis for the operations to evaluate in the correct sequence.
/// </remarks>
internal sealed class ShuntingYard<TOperator, TOperand>
{
  private readonly Parser parser;
  private readonly Tokenizer tokenizer;
  private readonly UnaryEval evalUnary;
  private readonly BinaryEval evalBinary;
  private readonly Dictionary<string, OperatorRegistration> operators = [];
  private readonly HashSet<char> operatorOpeners = [];

  private char startGroup;
  private char endGroup;
  private char escapeChar;
  private char startLiteralChar;
  private char endLiteralChar;

  public delegate TOperand Parser(string token);

  public delegate TOperand UnaryEval(TOperand operand, TOperator op);
  public delegate TOperand BinaryEval(TOperand lhs, TOperand rhs, TOperator op);

  public ShuntingYard(Parser parser, UnaryEval evalUnary = null, BinaryEval evalBinary = null)
  {
    this.parser = parser;
    this.evalUnary = evalUnary;
    this.evalBinary = evalBinary;
    SetGroupSymbols('(', ')');
    SetLiteralSymbols('\"', '\"');
    SetEscapeChar('\\');
    tokenizer = new Tokenizer(this);
  }

  public bool IgnoreWhitespace { get; set; } = true;

  public void SetGroupSymbols(char start, char end)
  {
    startGroup = start;
    endGroup = end;
  }

  public void SetLiteralSymbols(char start, char end)
  {
    startLiteralChar = start;
    endLiteralChar = end;
  }

  public void SetEscapeChar(char c)
  {
    escapeChar = c;
  }

  private bool VerifySymbols(out string failReason)
  {
    failReason = null;
    if (startGroup == endGroup)
    {
      failReason = "Start and open of group cannot be the same symbols";
      return false;
    }
    if (startLiteralChar == startGroup || startLiteralChar == endGroup ||
      endLiteralChar == startGroup || endLiteralChar == endGroup)
    {
      failReason = "Literal symbols cannot also be used as group symbols.";
      return false;
    }
    if (startGroup == escapeChar)
    {
      failReason = "Start group and escape character cannot be the same symbols.";
      return false;
    }
    if (endGroup == escapeChar)
    {
      failReason = "End group and escape character cannot be the same symbols.";
      return false;
    }
    if (startLiteralChar == escapeChar || endLiteralChar == escapeChar)
    {
      failReason = "Literal symbols cannot also be used as the escape character.";
      return false;
    }
    if (operatorOpeners.Contains(startGroup) || operatorOpeners.Contains(endGroup))
    {
      failReason = "Group symbols cannot also be used as operators.";
      return false;
    }
    if (operatorOpeners.Contains(startLiteralChar) || operatorOpeners.Contains(endLiteralChar))
    {
      failReason = "Literal symbols cannot also be used as operators.";
      return false;
    }
    if (operatorOpeners.Contains(escapeChar))
    {
      failReason = "Escape char cannot also be used as an operator.";
      return false;
    }
    return true;
  }

  public void AddBinaryOperator(string symbol, TOperator value, uint precedence = 0)
  {
    if (string.IsNullOrEmpty(symbol))
      throw new ArgumentException("symbol must be valid");

    OperatorRegistration registration = GetRegistration(symbol);
    if (registration.binary != null)
      throw new ArgumentException($"Binary operator {symbol} is already registered.");

    registration.binary = new Operator
    {
      value = value,
      precedence = precedence,
      type = Operator.Type.Binary
    };
  }

  public void AddUnaryOperator(string symbol, TOperator value, UnaryAffix unary, uint precedence = 0)
  {
    if (string.IsNullOrEmpty(symbol))
      throw new ArgumentException("symbol must be valid");
    if (unary == 0 || (unary & ~(UnaryAffix.Prefix | UnaryAffix.Postfix)) != 0)
      throw new ArgumentException("unary must specify a valid affix");

    OperatorRegistration registration = GetRegistration(symbol);
    if (registration.unary != null)
      throw new ArgumentException($"Unary operator {symbol} is already registered.");

    registration.unary = new Operator
    {
      value = value,
      precedence = precedence,
      type = Operator.Type.Unary,
      unary = unary
    };
  }

  private OperatorRegistration GetRegistration(string symbol)
  {
    if (!operators.TryGetValue(symbol, out OperatorRegistration registration))
    {
      registration = new OperatorRegistration();
      operators.Add(symbol, registration);
      operatorOpeners.Add(symbol[0]);
    }
    return registration;
  }

  public TOperand Evaluate(string expression)
  {
    if (!VerifySymbols(out string failReason))
      throw new InvalidOperationException(failReason);

    if (string.IsNullOrWhiteSpace(expression))
      return default;

    var tokens = tokenizer.Tokenize(expression);
    State state = new(this);
    foreach (Token token in tokens)
    {
      state.ProcessToken(token);
    }
    state.Flush();
    return state.Result;
  }

  private static bool IsNumericPrefix(char c)
  {
    return typeof(TOperand).IsNumericType() && c is '-' or '+';
  }

  [DebuggerDisplay("Token = {value}")]
  private class Token
  {
    public Type type;
    public string value;
    public Operator op;

    internal enum Type { Operator, Operand, OpenGroup, CloseGroup }
  }

  private struct Operator
  {
    public TOperator value;
    public Type type;
    public uint precedence;
    public UnaryAffix unary;

    internal enum Type { Unary, Binary, Group }
  }

  private sealed class OperatorRegistration
  {
    public Operator? binary;
    public Operator? unary;
  }

  private sealed class State(ShuntingYard<TOperator, TOperand> shuntingYard)
  {
    private readonly Stack<TOperand> output = [];
    private readonly Stack<Operator> holding = [];

    // Hoist delegates and operator cache
    private readonly Parser parser = shuntingYard.parser;
    private readonly UnaryEval evalUnary = shuntingYard.evalUnary;
    private readonly BinaryEval evalBinary = shuntingYard.evalBinary;

    public TOperand Result { get; private set; }

    public void Flush()
    {
      // Flush the rest of the holding stack, it should already be in order of precedence.
      while (holding.TryPop(out Operator top))
      {
        if (top.type == Operator.Type.Group)
          throw new ArgumentException("Unclosed group.");

        Evaluate(top);
      }

      if (output.Count != 1)
      {
        throw new InvalidOperationException(
          "Unable to evaluate expression, order of operations did not evaluate the entire expression.");
      }
      Result = output.Pop();
    }

    public void ProcessToken(Token token)
    {
      switch (token.type)
      {
        case Token.Type.Operator:
          InsertOperator(token.op);
          break;
        case Token.Type.Operand:
          output.Push(parser(token.value));
          break;
        case Token.Type.OpenGroup:
          holding.Push(new Operator { type = Operator.Type.Group });
          break;
        case Token.Type.CloseGroup:
        {
          while (holding.TryPop(out Operator top) && top.type != Operator.Type.Group)
          {
            Evaluate(top);
          }
          break;
        }
      }
    }

    private void InsertOperator(in Operator op)
    {
      if (op.type == Operator.Type.Unary && (op.unary & UnaryAffix.Prefix) != 0)
      {
        holding.Push(op);
        return;
      }

      while (holding.TryPeek(out Operator top) && top.type != Operator.Type.Group)
      {
        if (op.precedence > top.precedence)
          break;
        if (op.precedence == top.precedence && op.type == Operator.Type.Binary &&
          (top.unary & UnaryAffix.Postfix) != 0)
          break;

        Evaluate(holding.Pop());
      }
      holding.Push(op);
    }

    private void Evaluate(in Operator op)
    {
      TOperator value = op.value;
      TOperand result;
      switch (op.type)
      {
        case Operator.Type.Unary:
        {
          result = evalUnary(output.Pop(), value);
          break;
        }
        case Operator.Type.Binary:
        {
          TOperand rhs = output.Pop();
          TOperand lhs = output.Pop();
          result = evalBinary(lhs, rhs, value);
          break;
        }
        default:
          throw new NotSupportedException(op.type.ToString());
      }
      output.Push(result);
    }
  }

  private sealed class Tokenizer(ShuntingYard<TOperator, TOperand> shuntingYard)
  {
    private List<Token> tokens;
    private StringView buffer;
    private bool expectingOperand = true;
    private int groupDepth;
    private int index;

    private bool literalOperand;

    public List<Token> Tokenize(string expression)
    {
      tokens = [];
      buffer = new StringView(expression, start: 0, length: 0);
      for (; index < expression.Length; index++)
      {
        char c = expression[index];
        if (c == shuntingYard.escapeChar && index + 1 < expression.Length && IsEscapable(expression[index + 1]))
        {
          IncrementBuffer(characters: 2);
          index++;
          continue;
        }

        if (!literalOperand && c == shuntingYard.startLiteralChar)
        {
          StartLiteral();
          continue;
        }
        if (literalOperand && c == shuntingYard.endLiteralChar)
        {
          EndLiteral();
          continue;
        }
        if (literalOperand)
        {
          IncrementBuffer();
          continue;
        }

        if (char.IsWhiteSpace(c) &&
          (shuntingYard.IgnoreWhitespace || (buffer.IsEmpty && !shuntingYard.operatorOpeners.Contains(c))))
        {
          FlushBuffer();
          AdvanceBuffer();
          continue;
        }

        if (c == shuntingYard.startGroup)
        {
          OpenGroup();
          continue;
        }
        if (c == shuntingYard.endGroup)
        {
          CloseGroup();
          continue;
        }

        if (shuntingYard.operatorOpeners.Contains(c) || IsNumericPrefix(c))
        {
          ProcessOperator(c);
          continue;
        }
        IncrementBuffer();
      }
      FlushBuffer(trimTrailingWhitespace: true);

      if (expectingOperand || literalOperand || groupDepth != 0)
        throw new InvalidOperationException("Incomplete expression.");

      return tokens;
    }

    private void StartLiteral()
    {
      FlushBuffer(trimTrailingWhitespace: true);
      if (!expectingOperand)
        throw new InvalidOperationException("Missing operator before literal.");

      literalOperand = true;
      AdvanceBuffer();
    }

    private void EndLiteral()
    {
      FlushBuffer(trimTrailingWhitespace: true);
      if (expectingOperand)
        throw new InvalidOperationException("Unexpected end of literal.");

      literalOperand = false;
      AdvanceBuffer();
    }

    private void OpenGroup()
    {
      FlushBuffer(trimTrailingWhitespace: true);
      if (!expectingOperand)
        throw new InvalidOperationException("Missing operator before group.");

      tokens.Add(new Token { type = Token.Type.OpenGroup });
      groupDepth++;
      AdvanceBuffer();
    }

    private void CloseGroup()
    {
      FlushBuffer(trimTrailingWhitespace: true);
      if (expectingOperand || groupDepth == 0)
        throw new InvalidOperationException("Unexpected end of group.");

      tokens.Add(new Token { type = Token.Type.CloseGroup });
      groupDepth--;
      AdvanceBuffer();
    }

    private void ProcessOperator(char c)
    {
      if (IsNumericSign(c))
      {
        buffer.length++;
        return;
      }

      FlushBuffer(trimTrailingWhitespace: true);
      string symbol = MatchOperator();
      bool prefix = expectingOperand;
      Operator op = ResolveOperator(symbol, prefix);
      tokens.Add(new Token { type = Token.Type.Operator, value = symbol, op = op });

      index += symbol.Length - 1;
      Assert.IsTrue(buffer.IsEmpty);
      AdvanceBuffer();
      expectingOperand = prefix || op.type == Operator.Type.Binary;
    }

    private bool IsNumericSign(char c)
    {
      if (!expectingOperand || !buffer.IsEmpty || !IsNumericPrefix(c) || index + 1 >= buffer.source.Length)
        return false;
      if (!char.IsDigit(buffer.source[index + 1]) && buffer.source[index + 1] != '.')
        return false;

      return !shuntingYard.operators.TryGetValue(c.ToString(), out OperatorRegistration sign) ||
        sign.unary is not { unary: var signAffix } || (signAffix & UnaryAffix.Prefix) == 0;
    }

    private string MatchOperator()
    {
      string symbol = null;
      foreach (string candidate in shuntingYard.operators.Keys)
      {
        if (symbol != null && symbol.Length > candidate.Length)
          continue;
        if (candidate.Length + index > buffer.source.Length)
          continue;

        if (string.CompareOrdinal(buffer.source, index, candidate, 0, candidate.Length) == 0)
        {
          symbol = candidate;
        }
      }

      if (symbol == null)
        throw new InvalidOperationException($"Unknown operator at position {index}.");

      return symbol;
    }

    private Operator ResolveOperator(string symbol, bool prefix)
    {
      OperatorRegistration registration = shuntingYard.operators[symbol];
      Operator? op;
      if (prefix)
      {
        op = registration.unary is { unary: var prefixAffix } && (prefixAffix & UnaryAffix.Prefix) != 0 ?
          registration.unary : null;
      }
      else
      {
        op = registration.binary ??
          (registration.unary is { unary: var postfixAffix } && (postfixAffix & UnaryAffix.Postfix) != 0 ?
            registration.unary : null);
      }
      if (op == null)
        throw new InvalidOperationException($"Operator {symbol} is invalid at position {index}.");

      Operator resolved = op.Value;
      if (resolved.type == Operator.Type.Unary)
      {
        resolved.unary = prefix ? UnaryAffix.Prefix : UnaryAffix.Postfix;
      }
      return resolved;
    }

    private void IncrementBuffer(int characters = 1)
    {
      if (!expectingOperand && buffer.IsEmpty)
        throw new InvalidOperationException("Missing operator before operand.");

      buffer.length = Math.Min(buffer.length + characters, buffer.source.Length - buffer.start);
    }

    private bool IsEscapable(char character)
    {
      return character == shuntingYard.escapeChar || shuntingYard.operatorOpeners.Contains(character) ||
             character == shuntingYard.startLiteralChar || character == shuntingYard.endLiteralChar ||
             character == shuntingYard.startGroup || character == shuntingYard.endGroup;
    }

    private bool IsEscaped(int position)
    {
      int escapeCount = 0;
      for (int i = position - 1; i >= buffer.start && buffer.source[i] == shuntingYard.escapeChar; i--)
      {
        escapeCount++;
      }
      return (escapeCount & 1) != 0;
    }

    private string Unescape(string value)
    {
      if (value.IndexOf(shuntingYard.escapeChar) < 0)
        return value;

      StringBuilder unescaped = new(value.Length);
      for (int i = 0; i < value.Length; i++)
      {
        if (value[i] == shuntingYard.escapeChar && i + 1 < value.Length && IsEscapable(value[i + 1]))
        {
          i++;
        }
        unescaped.Append(value[i]);
      }
      return unescaped.ToString();
    }

    private void FlushBuffer(bool trimTrailingWhitespace = false)
    {
      if (trimTrailingWhitespace && !shuntingYard.IgnoreWhitespace)
      {
        while (!buffer.IsEmpty && char.IsWhiteSpace(buffer.source[buffer.start + buffer.length - 1]) &&
          !IsEscaped(buffer.start + buffer.length - 1))
        {
          buffer.length--;
        }
      }
      if (buffer.IsEmpty)
        return;

      tokens.Add(new Token { type = Token.Type.Operand, value = Unescape(buffer.ToString()) });
      buffer.length = 0;
      expectingOperand = false;
    }

    private void AdvanceBuffer()
    {
      buffer.start = index + 1;
    }
  }
}
