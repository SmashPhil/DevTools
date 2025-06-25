using System;

namespace DevTools.Benchmarking;

[AttributeUsage(AttributeTargets.Class)]
public class SampleSizeAttribute : MetaDataAttribute<int>
{
  public SampleSizeAttribute(int count) : base(MetaDataName.SampleSize, count)
  {
  }
}