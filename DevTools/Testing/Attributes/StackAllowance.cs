using JetBrains.Annotations;

namespace DevTools.Testing;

/// <summary>
/// Message patterns that are allowed only when the log's stack trace also matches <see cref="StackPattern"/>.
/// </summary>
[PublicAPI]
public sealed class StackAllowance
{
  internal StackAllowance(string stackPattern, string[] patterns)
  {
    StackPattern = stackPattern;
    Patterns = patterns;
  }

  public string StackPattern { get; }

  public string[] Patterns { get; }
}
