using System;
using JetBrains.Annotations;

namespace DevTools.UnitTesting;

[PublicAPI]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class LoadIfAnomalyActiveAttribute : LoadIfModsActiveAttribute
{
  public LoadIfAnomalyActiveAttribute() : base("ludeon.rimworld.anomaly")
  {
  }
}