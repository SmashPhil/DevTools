using UnityEngine;
using Verse;

namespace DevTools.Testing.Instructions;

public class WaitTicks : CustomYieldInstruction
{
    public override bool keepWaiting => tickManager.TicksGame < endTick;

    private readonly int endTick;
    private readonly TickManager tickManager;
    public WaitTicks(int ticks)
    {
        tickManager = Find.TickManager;
        endTick = tickManager.TicksGame + ticks;
    }
}