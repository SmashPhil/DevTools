using JetBrains.Annotations;
using RimWorld;

namespace DevTools.Testing;

/// <summary>
/// Requests a new game with the provided scenario before executing the fixture.
/// </summary>
[PublicAPI]
public interface IScenario
{
  /// <summary>
  /// Scenario used for new game generation.
  /// </summary>
  Scenario Scenario { get; }
}
