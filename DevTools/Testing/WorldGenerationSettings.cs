using RimWorld.Planet;

namespace DevTools.Testing;

public class WorldGenerationSettings
{
  public float percent = 0.05f;

  public OverallRainfall rainfall = OverallRainfall.Normal;
  public OverallTemperature temperature = OverallTemperature.Normal;
  public OverallPopulation population = OverallPopulation.Normal;
  public LandmarkDensity landmarkDensity = LandmarkDensity.Normal;
}