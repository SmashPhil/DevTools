using JetBrains.Annotations;
using UnityEngine.Assertions;
using Verse;

namespace DevTools;

[PublicAPI]
public static class MapUtils
{
  public static void KillEverything(Map map)
  {
    // We can't do a for loop here since events (e.g. Man In Black) can trigger on pawn deaths,
    // spawning another pawn on the map. Just keep going until everything is dead.
    const int MaxLimit = 9999;
    int attempts = MaxLimit;
    while (map.mapPawns.AllPawnsCount > 0 && attempts-- > 0)
    {
      map.mapPawns.AllPawns[0].Destroy();
    }

    if (attempts == 0)
    {
      const string ErrorMessage = "Unable to kill all pawns on map.";
#if DEBUG
      Assert.IsTrue(false, ErrorMessage);
#else
      Log.Error(ErrorMessage);
#endif
    }
  }
}
