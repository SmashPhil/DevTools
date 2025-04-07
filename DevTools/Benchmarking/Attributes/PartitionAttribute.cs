using System;

namespace DevTools.Benchmarking;

[AttributeUsage(AttributeTargets.Class)]
public class PartitionAttribute : Attribute
{
  public PartitionAttribute(int count)
  {
    Count = count;
  }

  public int Count { get; }
}