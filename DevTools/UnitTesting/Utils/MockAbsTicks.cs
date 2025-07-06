using System;
using JetBrains.Annotations;
using Verse;

namespace DevTools.UnitTesting;

/// <summary>
/// Temporarily mocks the <c>gameStartAbsTick</c> value in the global <see cref="TickManager"/>
/// to simulate a specific absolute-tick state for testing. Restores the original value when disposed.
/// </summary>
[PublicAPI]
public readonly struct MockAbsTicks : IDisposable
{
  private readonly TickManager tickManager;
  private readonly int gameStartAbsTick;

  /// <summary>
  /// Initializes a new <see cref="MockAbsTicks"/>, saving the current <c>gameStartAbsTick</c>
  /// and setting it to the specified <paramref name="gameStartAbsTick"/> value.
  /// </summary>
  /// <param name="gameStartAbsTick">
  /// The absolute tick count since game start to mock for the duration of the test.
  /// </param>
  public MockAbsTicks(int gameStartAbsTick)
  {
    tickManager = Find.TickManager;
    this.gameStartAbsTick = Find.TickManager.TicksAbs;
    Find.TickManager.gameStartAbsTick = gameStartAbsTick;
  }

  /// <summary>
  /// Restores the <c>gameStartAbsTick</c> in the global <see cref="TickManager"/>
  /// to its original value captured at construction.
  /// </summary>
  void IDisposable.Dispose()
  {
    tickManager.gameStartAbsTick = gameStartAbsTick;
  }
}