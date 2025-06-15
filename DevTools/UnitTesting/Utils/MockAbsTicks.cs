using System;
using JetBrains.Annotations;
using Verse;

namespace DevTools.UnitTesting;

[PublicAPI]
public readonly struct MockAbsTicks : IDisposable
{
  private readonly TickManager tickManager;
  private readonly int gameStartAbsTick;

  public MockAbsTicks(int gameStartAbsTick)
  {
    tickManager = Find.TickManager;
    this.gameStartAbsTick = Find.TickManager.TicksAbs;
    Find.TickManager.gameStartAbsTick = gameStartAbsTick;
  }

  void IDisposable.Dispose()
  {
    tickManager.gameStartAbsTick = gameStartAbsTick;
  }
}