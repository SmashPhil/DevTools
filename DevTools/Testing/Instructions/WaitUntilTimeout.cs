using System;
using JetBrains.Annotations;
using UnityEngine;
using Verse;

namespace DevTools.Testing;

[PublicAPI]
public class WaitUntilTimeout : CustomYieldInstruction
{
  private readonly Func<bool> condition;
  private readonly TickManager tickManager;
  private readonly int endTick;

  public WaitUntilTimeout(Func<bool> condition, int maxTicksToWait)
  {
    this.condition = condition;
    tickManager = Find.TickManager;
    endTick = tickManager.TicksGame + maxTicksToWait;
  }

  public override bool keepWaiting
  {
    get
    {
      if (tickManager.TicksGame > endTick)
      {
        Test.Fail("Timed out");
        return false;
      }
      return !condition();
    }
  }
}