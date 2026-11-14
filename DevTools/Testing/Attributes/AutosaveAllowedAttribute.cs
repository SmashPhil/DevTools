using System;
using JetBrains.Annotations;

namespace DevTools.Testing;

/// <summary>
/// Lets the game autosave while the annotated test fixture or function runs. Every other test
/// runs with autosave held, so a test run never writes its world into the player's Saves folder.
/// </summary>
[PublicAPI]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AutosaveAllowedAttribute : MetaDataAttribute<bool>
{
  /// <summary>
  /// Lets the game autosave while the annotated test fixture or function runs.
  /// </summary>
  public AutosaveAllowedAttribute() : base(MetaDataName.AutosaveAllowed, true)
  {
  }
}
