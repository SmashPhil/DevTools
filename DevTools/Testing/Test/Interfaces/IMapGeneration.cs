using JetBrains.Annotations;

namespace DevTools.Testing;

/// <summary>
/// Requests a new map before executing the fixture.
/// </summary>
[PublicAPI]
public interface IMapGeneration
{
  /// <summary>
  /// Settings used to generate the map.
  /// </summary>
  MapGenerationSettings MapGenerationSettings { get; }
}
