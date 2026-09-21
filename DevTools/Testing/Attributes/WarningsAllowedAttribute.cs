using System;
using JetBrains.Annotations;

namespace DevTools.Testing;

/// <summary>
/// Regex patterns of logged warnings that do not fail the annotated test fixture or function.
/// </summary>
[PublicAPI]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class WarningsAllowedAttribute : MetaDataAttribute<string[]>
{
  public WarningsAllowedAttribute(params string[] patterns) : base(MetaDataName.WarningsAllowed, patterns)
  {
  }
}
