using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

namespace DevTools.Testing;

public enum WaitJobType
{
  StartsJob,
  StartsAndFinishesJob,
  DoesntStartJob
}

[PublicAPI]
public sealed class WaitJob : CustomYieldInstruction
{
  private readonly Pawn pawn;
  private readonly JobDef expectedJob;
  private readonly WaitJobType waitJobType;

  private State state;
  private readonly int timeoutTick;
  private readonly int startTick;
  private int originalJobId;

  private enum State
  {
    WaitingForExpectedJob,
    WaitingForNextJob,
    Finished
  }

  public WaitJob(Pawn pawn, JobDef expectedJob, int timeoutTicks = GenDate.TicksPerHour,
      WaitJobType waitJobType = WaitJobType.StartsAndFinishesJob)
  {
    this.pawn = pawn;
    this.expectedJob = expectedJob;
    this.waitJobType = waitJobType;
    startTick = Find.TickManager.TicksGame;

    if (pawn.CurJob == null || pawn.CurJobDef != expectedJob)
    {
      state = State.WaitingForExpectedJob;
      timeoutTick = startTick + timeoutTicks;
    }
    else
    {
      originalJobId = pawn.CurJob.loadID;
      state = State.WaitingForNextJob;
    }
  }

  public override bool keepWaiting
  {
    get
    {
      switch (state)
      {
        case State.WaitingForExpectedJob:
        {
          if (Find.TickManager.TicksGame >= timeoutTick)
          {
            if (waitJobType != WaitJobType.DoesntStartJob)
              Test.Fail($"No job started after {timeoutTick - startTick} ticks");
            else
            {
              state = State.Finished;
              return false;
            }
          }

          if (pawn.CurJob == null || pawn.CurJobDef != expectedJob)
            return true;

          if (waitJobType == WaitJobType.StartsJob)
          {
            state = State.Finished;
            return false;
          }
          originalJobId = pawn.CurJob.loadID;
          state = State.WaitingForNextJob;
          return true;
        }
        case State.WaitingForNextJob:
        {
          if (pawn.CurJob == null)
            return true;

          if (waitJobType == WaitJobType.StartsAndFinishesJob && pawn.CurJob.loadID == originalJobId)
            return true;

          state = State.Finished;
          return false;
        }
        case State.Finished:
        default:
          return false;
      }
    }
  }
}