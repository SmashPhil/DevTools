using System;
using JetBrains.Annotations;

namespace DevTools.UnitTesting;

[PublicAPI]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class LoadIfAnyModsActiveAttribute : MetaDataAttribute<string[]>
{
  public LoadIfAnyModsActiveAttribute(params string[] packageIds) : base(
    MetaDataName.LoadIfAnyModsActive, packageIds)
  {
  }
}