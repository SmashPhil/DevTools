using System;
using JetBrains.Annotations;

namespace DevTools.UnitTesting;

[PublicAPI]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class LoadIfIdeologyActiveAttribute : LoadIfModsActiveAttribute
{
  public LoadIfIdeologyActiveAttribute() : base("ludeon.rimworld.ideology")
  {
  }
}