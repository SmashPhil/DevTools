using System.Collections.Generic;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace DevTools.Testing;

[PublicAPI]
public class MapGenerationSettings
{
  private const int DefaultSize = 150;

  public int size = DefaultSize;
  public BiomeDef biome;
  public MapGeneratorDef mapGeneratorDef;
  public List<GenStepWithParams> extraGenStepDefs;
  public Season season;

  internal static MapGenerationSettings Default;

  static MapGenerationSettings()
  {
    Default = new MapGenerationSettings
    {
      size = DefaultSize,
      biome = BiomeDefOf.TemperateForest,
      mapGeneratorDef = new MapGeneratorDef
      {
        defName = "DevTools_DefaultTestMap",
        genSteps = [
          new GenStepDef
          {
            defName = "DevTools_DefaultGenStep",
            order = 210, // Same as GenStepDef - Terrain
            genStep = new GenStep_EmptyMap()
          },
          DefDatabase<GenStepDef>.GetNamed("FindPlayerStartSpot")
        ]
      },
      season = Season.PermanentSummer
    };
  }
}