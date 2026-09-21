using System;
using JetBrains.Annotations;

namespace DevTools.Testing;

/// <summary>
/// Regex patterns of logged errors that do not fail the annotated test fixture or function.
/// </summary>
[PublicAPI]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ErrorsAllowedAttribute : MetaDataAttribute<string[]>
{
  public ErrorsAllowedAttribute(params string[] patterns) : base(MetaDataName.ErrorsAllowed, patterns)
  {
  }
}
