using System;
using JetBrains.Annotations;

namespace DevTools.UnitTesting;

[PublicAPI]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class LoadIfModsActiveAttribute : MetaDataAttribute
{
  public LoadIfModsActiveAttribute(params string[] packageIds) : base(
    MetaDataName.LoadIfAllModsActive, packageIds)
  {
  }
}