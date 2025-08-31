using System;
using JetBrains.Annotations;

namespace DevTools.Testing;

[PublicAPI]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class LoadIfOdysseyActiveAttribute : LoadIfModsActiveAttribute
{
  public LoadIfOdysseyActiveAttribute() : base("ludeon.rimworld.odyssey")
  {
  }
}