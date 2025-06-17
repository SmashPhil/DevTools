using System;
using JetBrains.Annotations;

namespace DevTools.UnitTesting;

[PublicAPI]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class LoadIfAnyModsActiveAttribute : MetaDataAttribute
{
  public LoadIfAnyModsActiveAttribute(params string[] packageIds) : base(
    MetaDataName.LoadIfAnyModsActive, packageIds)
  {
  }
}