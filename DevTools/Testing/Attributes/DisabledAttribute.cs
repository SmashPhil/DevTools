using System;

namespace DevTools.Testing;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class DisabledAttribute : MetaDataAttribute<bool>
{
  public DisabledAttribute() : base(MetaDataName.Disabled, true)
  {
  }
}