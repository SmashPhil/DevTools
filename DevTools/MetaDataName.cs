namespace DevTools;

internal static class MetaDataName
{
  // Benchmarking
  public static readonly int SampleSize = "SampleSize".GetHashCode();

  // Unit Testing
  public static readonly int Category = "Category".GetHashCode();
  public static readonly int Description = "Description".GetHashCode();
  public static readonly int ExecutionPriority = "ExecutionPriority".GetHashCode();
  public static readonly int LoadSave = "LoadSave".GetHashCode();
}