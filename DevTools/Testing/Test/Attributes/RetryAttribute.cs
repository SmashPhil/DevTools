using System;

namespace DevTools.Testing;

/// <summary>
/// Specifies that a test method should be retried when it fails.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class RetryAttribute : MetaDataAttribute<ushort>
{
  /// <summary>
  /// Specifies that a test method should be retried when it fails.
  /// </summary>
  /// <param name="times">Number of retry attempts.</param>
  public RetryAttribute(ushort times) : base(MetaDataName.RetryTest, times)
  {
  }
}
