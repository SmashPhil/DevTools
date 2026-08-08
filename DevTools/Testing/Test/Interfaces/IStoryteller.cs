using JetBrains.Annotations;
using RimWorld;

namespace DevTools.Testing;

/// <summary>
/// Requests a new game with the provided storyteller before executing the fixture.
/// </summary>
[PublicAPI]
public interface IStoryteller
{
  /// <summary>
  /// Storyteller used for new game generation.
  /// </summary>
  Storyteller Storyteller { get; }
}
