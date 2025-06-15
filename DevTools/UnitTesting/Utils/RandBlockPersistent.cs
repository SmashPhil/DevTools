using System;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using Verse;

namespace DevTools;

/// <summary>
/// RandBlock implementation that circumvents the state stack so it can persist across frames.
/// </summary>
/// <remarks>
/// RimWorld dumps the state stack every frame to ensure mods aren't messing with rand state consistency,
/// but this makes getting consistent test results via seeds difficult to accomplish with the state stack.
/// </remarks>
[PublicAPI]
public readonly struct RandBlockPersistent : IDisposable
{
  private static readonly FieldInfo randSeedField;

  private readonly uint prevSeed;

  static RandBlockPersistent()
  {
    randSeedField = AccessTools.Field(typeof(Rand), "seed");
  }

  public RandBlockPersistent(uint seed)
  {
    Rand.EnsureStateStackEmpty();
    prevSeed = (uint)randSeedField.GetValue(null);
    randSeedField.SetValue(null, seed);
  }

  void IDisposable.Dispose()
  {
    randSeedField.SetValue(null, prevSeed);
  }
}