using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DevTools.Benchmarking;

internal static class HighPerfCounter
{
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static long ElapsedTicks(long current)
  {;
    return Stopwatch.GetTimestamp() - current;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static long ElapsedMilliseconds(long current)
  {
    long elapsedTicks = ElapsedTicks(current);
    return Benchmark.ToMilliseconds(elapsedTicks);
  }
}