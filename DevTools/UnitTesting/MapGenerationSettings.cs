using System.Collections.Generic;
using RimWorld;
using Verse;

namespace DevTools.UnitTesting;

public class MapGenerationSettings
{
  public int size = 150;
  public BiomeDef biome;
  public MapGeneratorDef mapGeneratorDef;
  public List<GenStepWithParams> extraGenStepDefs;
  public bool isPocketMap = false;
  public Season season;
}