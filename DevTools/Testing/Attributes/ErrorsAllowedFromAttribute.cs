using System;
using JetBrains.Annotations;

namespace DevTools.Testing;

/// <summary>
/// Regex patterns of logged errors that do not fail the annotated test fixture or function when the error's stack
/// trace also matches <c>stackPattern</c>.
/// Repeat it for each source; repeated and inherited allowances all apply.
/// </summary>
[PublicAPI]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class ErrorsAllowedFromAttribute : MetaDataAttribute<StackAllowance>
{
  public ErrorsAllowedFromAttribute(string stackPattern, params string[] patterns)
    : base(MetaDataName.ErrorsAllowedFrom, new StackAllowance(stackPattern, patterns))
  {
  }
}
