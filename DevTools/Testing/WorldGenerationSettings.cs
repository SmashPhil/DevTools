using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace DevTools.Testing;

public class WorldGenerationSettings
{
  public float percent = 0.05f;

  public Season startingSeason = Season.Summer;
  public List<PawnKindCount> startingPawnsRequired;
  public List<XenotypeCount> startingXenotypesRequired;
  public List<MutantCount> startingMutantsRequired;
  public DevelopmentalStage allowedDevelopmentalStages =
    DevelopmentalStage.Baby | DevelopmentalStage.Child | DevelopmentalStage.Adult;
  public List<SkillDef> startingSkillsRequired;

  public OverallRainfall rainfall = OverallRainfall.Normal;
  public OverallTemperature temperature = OverallTemperature.Normal;
  public OverallPopulation population = OverallPopulation.Normal;
  public LandmarkDensity landmarkDensity = LandmarkDensity.Normal;
}