using System;
using JetBrains.Annotations;

namespace DevTools.Testing;

/// <summary>
/// Regex patterns of logged errors that do not fail the annotated test fixture or function, but only when the
/// error's stack trace also matches <c>stackPattern</c>, such as a method of the mod that causes it.
/// </summary>
[PublicAPI]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ErrorsAllowedFromAttribute : MetaDataAttribute<(string stackPattern, string[] patterns)>
{
  public ErrorsAllowedFromAttribute(string stackPattern, params string[] patterns)
    : base(MetaDataName.ErrorsAllowedFrom, (stackPattern, patterns))
  {
  }
}
