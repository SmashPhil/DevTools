using System;
using JetBrains.Annotations;

namespace DevTools.UnitTesting;

[AttributeUsage(AttributeTargets.Method)]
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class ExecutionPriorityAttribute : Attribute, IMetaData
{
  public ExecutionPriorityAttribute(int priority)
  {
    Key = MetaDataName.ExecutionPriority;
    Value = priority;
  }

  public ExecutionPriorityAttribute(Priority priority) : this((int)priority)
  {
  }

  public int Key { get; }

  public object Value { get; }
}