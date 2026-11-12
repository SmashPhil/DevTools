using System;
using JetBrains.Annotations;

namespace DevTools.Testing;

/// <summary>
/// Regex patterns of logged warnings that do not fail the annotated test fixture or function when the warning's
/// stack trace also matches <c>stackPattern</c>.
/// </summary>
[PublicAPI]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class WarningsAllowedFromAttribute : MetaDataAttribute<StackAllowance>
{
  public WarningsAllowedFromAttribute(string stackPattern, params string[] patterns)
    : base(MetaDataName.WarningsAllowedFrom, new StackAllowance(stackPattern, patterns))
  {
  }
}
