using System;
using JetBrains.Annotations;
using Verse;

namespace DevTools.UnitTesting;

/// <summary>
/// Captures an Entity reference and destroys it when this object goes out of scope.
/// </summary>
[PublicAPI]
public readonly struct ScopeEntity : IDisposable
{
  private readonly Thing entity;

  public ScopeEntity(Thing entity)
  {
    this.entity = entity;
  }

  void IDisposable.Dispose()
  {
    if (!entity.Destroyed)
      entity.Destroy();
  }
}