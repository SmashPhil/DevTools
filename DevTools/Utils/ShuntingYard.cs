using System;
using System.Collections.Generic;

namespace DevTools;

internal class ShuntingYard<TOperand, TOperator>
{
  private Stack<TOperand> operands = [];
  private Stack<TOperator> operators = [];

  public ShuntingYard(Func<TOperator, uint> operatorPrecedence)
  {
  }
}