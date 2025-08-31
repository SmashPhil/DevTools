using System;
using JetBrains.Annotations;

namespace DevTools.Testing;

[PublicAPI]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class LoadIfIdeologyActiveAttribute : LoadIfModsActiveAttribute
{
  public LoadIfIdeologyActiveAttribute() : base("ludeon.rimworld.ideology")
  {
  }
}