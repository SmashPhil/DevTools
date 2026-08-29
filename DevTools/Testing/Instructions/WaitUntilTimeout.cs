using System;
using UnityEngine;
using Verse;

namespace DevTools.Testing.Instructions;

public class WaitUntilTimeout : CustomYieldInstruction
{
    private readonly Func<bool> predicate;
    private readonly TickManager tickManager;
    private readonly int endTick;
    
    public override bool keepWaiting
    {
        get
        {
            var result = !predicate();
            if(tickManager.TicksGame < endTick && result)
                Test.Fail("Timed out");
            return result;
        }
    }

    public WaitUntilTimeout(Func<bool> predicate, int ticks = 100)
    {
        this.predicate = predicate;
        tickManager = Find.TickManager;
        endTick = tickManager.TicksGame + ticks;
    }
}