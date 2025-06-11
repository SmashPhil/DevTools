using Verse;

namespace DevTools;

[StaticConstructorOnStartup]
internal static class MetaDataName
{
  // Benchmarking
  public static readonly int SampleSize = "SampleSize".GetHashCode();
  public static readonly int Measurement = "Measurement".GetHashCode();
  public static readonly int Table = "Table".GetHashCode();
  public static readonly int Graph = "Graph".GetHashCode();

  // Unit Testing
  public static readonly int Disabled = "Disabled".GetHashCode();
  public static readonly int Category = "Category".GetHashCode();
  public static readonly int Property = "Property".GetHashCode();
  public static readonly int Description = "Description".GetHashCode();
  public static readonly int ExecutionPriority = "ExecutionPriority".GetHashCode();
  public static readonly int LoadSave = "LoadSave".GetHashCode();
}