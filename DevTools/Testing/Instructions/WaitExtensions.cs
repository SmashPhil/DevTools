using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace DevTools.Testing;

[PublicAPI]
public static class WaitExtensions
{
  public static WaitJob WaitJob(this Pawn pawn, JobDef expectedJob, int timeoutTicks = GenDate.TicksPerHour,
    WaitJobType waitJobType = WaitJobType.StartsAndFinishesJob)
  {
    return new WaitJob(pawn, expectedJob, timeoutTicks, waitJobType);
  }
}