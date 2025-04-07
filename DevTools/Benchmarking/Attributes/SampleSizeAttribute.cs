using System;

namespace DevTools.Benchmarking;

[AttributeUsage(AttributeTargets.Class)]
public class SampleSizeAttribute : Attribute
{
  public SampleSizeAttribute(int count)
  {
    Count = count;
  }

  public int Count { get; }
}