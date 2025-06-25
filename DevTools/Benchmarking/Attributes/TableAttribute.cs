using System;

namespace DevTools.Benchmarking;

[Flags]
public enum Stat
{
  None = 0,
  Total = 1 << 0,
  Mean = 1 << 1,
  Median = 1 << 2,
  StdDev = 1 << 3
}

[AttributeUsage(AttributeTargets.Class)]
public class TableAttribute : MetaDataAttribute<Stat>
{
  public TableAttribute(Stat stats) : base(MetaDataName.Table, stats)
  {
  }
}