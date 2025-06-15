using UnityEngine;
using Verse;

namespace DevTools;

public class WaitForTicks : CustomYieldInstruction
{
  private readonly int ticks;
  private readonly int startTick;

  public WaitForTicks(int ticks)
  {
    this.ticks = ticks;
    this.startTick = Find.TickManager.TicksGame;
  }

  public override bool keepWaiting => Find.TickManager.TicksGame < startTick + ticks;
}