using System;
using JetBrains.Annotations;
using RimWorld.Planet;

namespace DevTools.UnitTesting;

/// <summary>
/// Captures a WorldObject reference and destroys it when this object goes out of scope.
/// </summary>
[PublicAPI]
public readonly struct ScopeWorldObject : IDisposable
{
  private readonly WorldObject worldObject;

  public ScopeWorldObject(WorldObject worldObject)
  {
    this.worldObject = worldObject;
  }

  void IDisposable.Dispose()
  {
    if (!worldObject.Destroyed)
      worldObject.Destroy();
  }
}