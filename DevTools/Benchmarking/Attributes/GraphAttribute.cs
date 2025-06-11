using System;

namespace DevTools.Benchmarking;

public enum GraphType
{
  None,
  Bar,
  Line,
  Scatter,
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class GraphAttribute : MetaDataAttribute
{
  public GraphAttribute(GraphType graphType) : base(MetaDataName.Graph, graphType)
  {
  }
}