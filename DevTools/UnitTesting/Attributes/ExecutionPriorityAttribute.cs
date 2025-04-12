using System;

namespace DevTools.UnitTesting;

[AttributeUsage(AttributeTargets.Method)]
public class ExecutionPriorityAttribute : Attribute
{
  public ExecutionPriorityAttribute(Priority priority)
  {
    Priority = (int)priority;
  }

  public ExecutionPriorityAttribute(int priority)
  {
    Priority = priority;
  }

  public int Priority { get; }
}