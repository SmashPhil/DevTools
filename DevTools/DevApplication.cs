using Verse;
using Waylith.UnityCompat;

namespace DevTools;

internal static class ExitCode
{
  public const int Pass = 0;
  public const int Fail = 1;
  public const int NoTests = 2;
}

internal static class DevApplication
{
  public static void Exit(int exitCode)
  {
    UnityRuntime.Current?.App.ExitCode = exitCode;
    Root.Shutdown();
  }
}
