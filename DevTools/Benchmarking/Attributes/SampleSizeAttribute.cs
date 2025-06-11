using System;

namespace DevTools.Benchmarking;

[AttributeUsage(AttributeTargets.Class)]
public class SampleSizeAttribute : MetaDataAttribute
{
  public SampleSizeAttribute(int count) : base(MetaDataName.SampleSize, count)
  {
  }
}