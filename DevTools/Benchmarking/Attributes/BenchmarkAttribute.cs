using System;
using JetBrains.Annotations;

namespace DevTools.Benchmarking;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
[AttributeUsage(AttributeTargets.Method)]
public class BenchmarkAttribute : Attribute
{
  public string Label { get; set; }
}