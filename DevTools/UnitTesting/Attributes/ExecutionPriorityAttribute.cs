using System;
using JetBrains.Annotations;

namespace DevTools.UnitTesting;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class ExecutionPriorityAttribute : MetaDataAttribute<int>
{
  public ExecutionPriorityAttribute(int priority) : base(MetaDataName.ExecutionPriority, priority)
  {
  }

  public ExecutionPriorityAttribute(Priority priority) : this((int)priority)
  {
  }
}