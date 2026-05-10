using System;

namespace DevTools.Testing;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
internal class HideInUIAttribute : MetaDataAttribute<bool>
{
  public HideInUIAttribute() : base(MetaDataName.HideInUI, true)
  {
  }
}
