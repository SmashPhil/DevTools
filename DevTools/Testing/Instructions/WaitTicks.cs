using JetBrains.Annotations;
using UnityEngine;
using Verse;

namespace DevTools.Testing;

[PublicAPI]
public class WaitTicks : CustomYieldInstruction
{
  private readonly int endTick;
  private readonly TickManager tickManager;

  public WaitTicks(int ticks)
  {
    tickManager = Find.TickManager;
    endTick = tickManager.TicksGame + ticks;
  }

  public override bool keepWaiting => tickManager.TicksGame < endTick;
}