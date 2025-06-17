using System;
using JetBrains.Annotations;

namespace DevTools.UnitTesting;

[PublicAPI]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class LoadIfRoyaltyActiveAttribute : LoadIfModsActiveAttribute
{
  public LoadIfRoyaltyActiveAttribute() : base("ludeon.rimworld.royalty")
  {
  }
}