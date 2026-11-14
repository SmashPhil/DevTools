using System;
using JetBrains.Annotations;

namespace DevTools.Testing;

/// <summary>
/// Allows the game to autosave while the annotated test fixture or function runs.
/// </summary>
[PublicAPI]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AutosaveAllowedAttribute : MetaDataAttribute<bool>
{
  public AutosaveAllowedAttribute() : base(MetaDataName.AutosaveAllowed, true)
  {
  }
}
