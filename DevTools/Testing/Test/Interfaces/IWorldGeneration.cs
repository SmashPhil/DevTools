using JetBrains.Annotations;

namespace DevTools.Testing;

/// <summary>
/// Requests a new world with the provided settings before executing the fixture.
/// </summary>
[PublicAPI]
public interface IWorldGeneration
{
  /// <summary>
  /// Settings used to generate the world.
  /// </summary>
  WorldGenerationSettings WorldGenerationSettings { get; }
}
