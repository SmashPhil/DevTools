using System;

namespace DevTools.UnitTesting;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class TestPriorityAttribute : Attribute
{
  public TestPriorityAttribute(ExecutionPriority priority)
  {
    Priority = (int)priority;
  }

  public TestPriorityAttribute(int priority)
  {
    Priority = priority;
  }

  public int Priority { get; }
}