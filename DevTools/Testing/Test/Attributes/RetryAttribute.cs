using System;

namespace DevTools.Testing;

/// <summary>
/// Specifies that a method should be retried automatically when it fails.
/// </summary>
/// <remarks>
/// Apply this attribute to a method to indicate that it supports automatic retry logic.
/// This attribute is typically used for flaky tests.
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public class RetryAttribute : MetaDataAttribute<ushort>
{
  public RetryAttribute(ushort times) : base(MetaDataName.RetryTest, times)
  {
  }
}