using System;
using Verse;

namespace DevTools;

internal abstract class DevItem
{
  public readonly ModContentPack mod;

  public abstract string Label { get; }

  public abstract DevItemType Type { get; }

  protected DevItem(ModContentPack mod)
  {
    this.mod = mod;
  }

  public abstract bool TryRegister(Type type);

  public enum DevItemType
  {
    Benchmarking,
    UnitTesting,
    StartupActions,
    Profiling,
  }
}