using System;
using JetBrains.Annotations;

namespace DevTools.Testing;

[PublicAPI]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class LoadIfAnomalyActiveAttribute : LoadIfModsActiveAttribute
{
  public LoadIfAnomalyActiveAttribute() : base("ludeon.rimworld.anomaly")
  {
  }
}