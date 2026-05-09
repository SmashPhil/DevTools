using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace DevTools.Testing;

[UsedImplicitly]
public class GenStep_EmptyMap : GenStep_Terrain
{
  public override void Generate(Map map, GenStepParams parms)
  {
    using var dis = map.pathing.DisableIncrementalScope();

    TerrainGrid terrainGrid = map.terrainGrid;
    foreach (IntVec3 allCell in map.AllCells)
    {
      terrainGrid.SetTerrain(allCell, TerrainDefOf.Concrete);
    }
  }
}
