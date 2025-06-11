using System;

namespace DevTools.Benchmarking;

[AttributeUsage(AttributeTargets.Class)]
public class MeasurementAttribute : MetaDataAttribute
{
  public MeasurementAttribute(Benchmark.Measurement measurement) : base(MetaDataName.Measurement,
    measurement)
  {
  }
}