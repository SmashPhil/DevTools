using System;
using JetBrains.Annotations;
using LudeonTK;

namespace DevTools.Benchmarking;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
[AttributeUsage(AttributeTargets.Class)]
public class BenchmarkClassAttribute : Attribute
{
  public BenchmarkClassAttribute()
  {
  }

  public BenchmarkClassAttribute(string category)
  {
    Category = category;
  }

  public string Category { get; }

  public bool RunAsync { get; set; } = true;

  public AllowedGameStates AllowedGameStates { get; set; }
}