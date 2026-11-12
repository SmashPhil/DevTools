using System;
using JetBrains.Annotations;

namespace DevTools.Testing;

/// <summary>
/// Regex patterns of logged errors that do not fail the annotated test fixture or function when the error's stack
/// trace also matches <c>stackPattern</c>.
/// </summary>
[PublicAPI]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ErrorsAllowedFromAttribute : MetaDataAttribute<StackAllowance>
{
  public ErrorsAllowedFromAttribute(string stackPattern, params string[] patterns)
    : base(MetaDataName.ErrorsAllowedFrom, new StackAllowance(stackPattern, patterns))
  {
  }
}
