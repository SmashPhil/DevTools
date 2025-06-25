using System;

namespace DevTools.Benchmarking;

[AttributeUsage(AttributeTargets.Class)]
public class MeasurementAttribute : MetaDataAttribute<Benchmark.Measurement>
{
  public MeasurementAttribute(Benchmark.Measurement measurement) : base(MetaDataName.Measurement,
    measurement)
  {
  }
}