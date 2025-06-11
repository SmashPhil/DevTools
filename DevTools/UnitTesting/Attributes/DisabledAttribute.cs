using System;

namespace DevTools.UnitTesting;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class DisabledAttribute : MetaDataAttribute
{
  public DisabledAttribute() : base(MetaDataName.Disabled, true)
  {
  }
}