using System;
using HarmonyLib;
using JetBrains.Annotations;
using Verse;

namespace DevTools;

/// <summary>
/// A <see cref="RandBlock"/> implementation that circumvents the state stack so it can persist across frames.
/// </summary>
/// <remarks>
/// RimWorld clears the RNG state stack every frame to prevent mods from introducing nondeterministic behavior.
/// However, this behavior interferes with tests that rely on a fixed seed. <see cref="RandBlockPersistent"/>
/// circumvents the stack mechanism to maintain a stable seed until disposed.
/// </remarks>
[PublicAPI]
public readonly struct RandBlockPersistent : IDisposable
{
  private static readonly AccessTools.FieldRef<uint> randSeed;
  /// <summary>
  /// The previously stored RNG seed to restore on disposal.
  /// </summary>
  private readonly uint prevSeed;

  static RandBlockPersistent()
  {
    randSeed = AccessTools.StaticFieldRefAccess<uint>(AccessTools.Field(typeof(Rand), "seed"));
  }

  /// <summary>
  /// Initializes a new <see cref="RandBlockPersistent"/>, clearing the state stack and setting
  /// the RNG seed to the specified value.
  /// </summary>
  /// <param name="seed">The RNG seed value to apply.</param>
  public RandBlockPersistent(uint seed)
  {
    Rand.EnsureStateStackEmpty();
    prevSeed = randSeed.Invoke();
    randSeed.Invoke() = seed;
  }

  /// <summary>
  /// Restores the RNG seed to its original value and re-enables the normal state stack behavior.
  /// </summary>
  void IDisposable.Dispose()
  {
    randSeed.Invoke() = prevSeed;
  }
}