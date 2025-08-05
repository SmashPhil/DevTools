using System;

namespace DevTools.Benchmarking;

// TODO 1.7 - Remove
[AttributeUsage(AttributeTargets.Class)]
[Obsolete("Sample size and partitions are calculated automatically now.", error: true)]
public class SampleSizeAttribute : MetaDataAttribute<int>
{
  public SampleSizeAttribute(int count) : base(MetaDataName.SampleSize, count)
  {
  }
}