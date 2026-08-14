using System;
using JetBrains.Annotations;

namespace DevTools.Benchmarking;

/// <summary>
/// Specifies the unit used to display benchmark measurements.
/// </summary>
[PublicAPI]
[AttributeUsage(AttributeTargets.Class)]
public class MeasurementAttribute : MetaDataAttribute<Benchmark.Measurement>
{
  /// <param name="measurement">Unit used to display benchmark measurements.</param>
  public MeasurementAttribute(Benchmark.Measurement measurement) : base(MetaDataName.Measurement,
    measurement)
  {
  }
}
