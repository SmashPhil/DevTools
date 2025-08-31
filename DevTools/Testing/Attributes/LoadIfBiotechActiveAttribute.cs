using System;
using JetBrains.Annotations;

namespace DevTools.Testing;

[PublicAPI]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class LoadIfBiotechActiveAttribute : LoadIfModsActiveAttribute
{
  public LoadIfBiotechActiveAttribute() : base("ludeon.rimworld.biotech")
  {
  }
}