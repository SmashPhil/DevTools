using System;
using JetBrains.Annotations;

namespace DevTools.UnitTesting;

[PublicAPI]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class LoadIfBiotechActiveAttribute : LoadIfModsActiveAttribute
{
  public LoadIfBiotechActiveAttribute() : base("ludeon.rimworld.biotech")
  {
  }
}