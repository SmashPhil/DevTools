using RimWorld;
using Verse;

namespace DevTools.Testing.Instructions;

public static class WaitExtensions
{
    public static WaitJob WaitJob(this Pawn p, JobDef expectedJob, int timeoutTicks = GenDate.TicksPerHour, WaitJobType waitJobType = WaitJobType.StartsAndFinishesJob) => 
        new(p, expectedJob, timeoutTicks, waitJobType);
}