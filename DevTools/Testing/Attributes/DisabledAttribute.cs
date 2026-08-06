using System;

namespace DevTools.Testing;

/// <summary>
/// Disables test fixture or function from being run.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class DisabledAttribute : MetaDataAttribute<bool>
{
  /// <summary>
  /// Disables test fixture or function from being run.
  /// </summary>
  public DisabledAttribute() : base(MetaDataName.Disabled, true)
  {
  }
}