using System;

namespace DevTools.UnitTesting;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class DisabledAttribute : MetaDataAttribute<bool>
{
  public DisabledAttribute() : base(MetaDataName.Disabled, true)
  {
  }
}