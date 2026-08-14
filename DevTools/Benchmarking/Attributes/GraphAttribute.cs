using System;

namespace DevTools.Benchmarking;

/// <summary>
/// Graph formats available for benchmark results.
/// </summary>
public enum GraphType
{
  /// <summary>
  /// Does not display benchmark results in a graph.
  /// </summary>
  None,

  /// <summary>
  /// Displays benchmark results in a bar graph.
  /// </summary>
  Bar,

  /// <summary>
  /// Displays benchmark results in a line graph.
  /// </summary>
  Line,

  /// <summary>
  /// Displays benchmark results in a scatter graph.
  /// </summary>
  Scatter,
}

/// <summary>
/// Specifies the graph format used to display benchmark results.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class GraphAttribute : MetaDataAttribute<GraphType>
{
  /// <summary>
  /// Initializes the attribute with the provided graph format.
  /// </summary>
  /// <param name="graphType">Graph format used to display benchmark results.</param>
  public GraphAttribute(GraphType graphType) : base(MetaDataName.Graph, graphType)
  {
    throw new NotImplementedException();
  }
}
