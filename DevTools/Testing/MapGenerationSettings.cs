using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine.Assertions;
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

  public List<Action> postGenerationActions = [];

  [Unsaved]
  private bool factionOverridden;

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
      }
    };
    Default.postGenerationActions.Add(ClearMap);
  }

  // ----------- IMapGeneration-only settings -----------

  /// <summary>
  /// Faction override for map being generated.
  /// </summary>
  /// <remarks>
  /// Assigning this overrides with whatever value is provided, including a null faction.
  /// If left unassigned, the faction will default to <see cref="Faction.OfPlayer"/>
  /// </remarks>
  public Faction Faction
  {
    get
    {
      if (factionOverridden)
        return field;

      return field ?? Faction.OfPlayer;
    }
    set
    {
      field = value;
      factionOverridden = true;
    }
  }

  /// <summary>
  /// Clear the entire map of all entities, we should be testing with a blank slate.
  /// </summary>
  private static void ClearMap()
  {
    // NOTE: Some mods spawn stuff post map-generation with no regard for gen steps. This bypasses
    // our blank map generation which can cause intermittent failures.
    Map map = Find.CurrentMap;
    Assert.IsNotNull(map);
    foreach (IntVec3 cell in CellRect.WholeMap(map))
    {
      map.roofGrid.SetRoof(cell, null);
    }
    foreach (IntVec3 cell in CellRect.WholeMap(map))
    {
      foreach (Thing thing in cell.GetThingList(map).ToList())
      {
        if (thing is not Pawn)
        {
          thing.Destroy();
        }
      }
    }
  }
}