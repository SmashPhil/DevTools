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
  private static readonly AccessTools.FieldRef<uint> randIterations;

  /// <summary>
  /// The previously stored RNG seed to restore on disposal.
  /// </summary>
  private readonly uint prevSeed;

  /// <summary>
  /// The previously stored iterations with an empty state stack.
  /// </summary>
  private readonly uint prevIterations;

  static RandBlockPersistent()
  {
    randSeed = AccessTools.StaticFieldRefAccess<uint>(AccessTools.Field(typeof(Rand), "seed"));
    randIterations = AccessTools.StaticFieldRefAccess<uint>(AccessTools.Field(typeof(Rand), "iterations"));
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
    prevIterations = randIterations.Invoke();

    randSeed.Invoke() = seed;
    randIterations.Invoke() = 0;
  }

  /// <summary>
  /// Current seed pushed to <see cref="Rand"/>
  /// </summary>
  public static uint CurrentSeed => randSeed.Invoke();

  /// <summary>
  /// Current iterations set in <see cref="Rand"/>
  /// </summary>
  public static uint CurrentIterations
  {
    get
    {
      return randIterations.Invoke();
    }
    internal set
    {
      randIterations.Invoke() = value;
    }
  }

  /// <summary>
  /// Restores the RNG seed to its original value and re-enables the normal state stack behavior.
  /// </summary>
  void IDisposable.Dispose()
  {
    randSeed.Invoke() = prevSeed;
    randIterations.Invoke() = prevIterations;
  }
}