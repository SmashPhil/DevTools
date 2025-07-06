using System;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine.Assertions;
using Verse;

namespace DevTools.UnitTesting;

/// <summary>
/// Temporarily mocks the <see cref="TickManager.TicksGame"/> value setting it to a specified
/// tick count for testing purposes and restoring the original value when disposed.
/// </summary>
[PublicAPI]
public readonly struct MockGameTicks : IDisposable
{
  // Cached FieldInfo for the internal <c>ticksGameInt</c> field of <see cref="TickManager"/>.
  private static readonly FieldInfo TicksGameField;

  private readonly TickManager tickManager;
  private readonly int ticksGame;

  /// <summary>
  /// Static constructor locates the private <c>ticksGameInt</c> field on <see cref="TickManager"/>.
  /// </summary>
  static MockGameTicks()
  {
    TicksGameField = AccessTools.Field(typeof(TickManager), "ticksGameInt");
    Assert.IsNotNull(TicksGameField);
  }

  /// <summary>
  /// Saves the current <see cref="TickManager.TicksGame"/> and overrides it with the provided
  /// <paramref name="ticksGame"/> value.
  /// </summary>
  /// <param name="ticksGame">
  /// The mock tick count to set for <see cref="TickManager.TicksGame"/> during the test.
  /// </param>
  public MockGameTicks(int ticksGame)
  {
    tickManager = Find.TickManager;
    this.ticksGame = Find.TickManager.TicksGame;
    TicksGameField.SetValue(tickManager, ticksGame);
  }

  /// <summary>
  /// Restores the <see cref="TickManager.TicksGame"/> value to its original
  /// state when <see cref="MockGameTicks"/> is disposed.
  /// </summary>
  void IDisposable.Dispose()
  {
    TicksGameField.SetValue(tickManager, ticksGame);
  }
}