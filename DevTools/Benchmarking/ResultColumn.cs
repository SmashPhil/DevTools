using System;
using JetBrains.Annotations;
using UnityEngine;

namespace DevTools.Benchmarking;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal class ResultColumn : IDataColumn
{
  public ResultColumn(ColumnType type, Stat stat, float width, float padding = 5)
  {
    Type = type;
    Stat = stat;
    Width = width;
    Padding = padding;
  }

  public ColumnType Type { get; }

  public Stat Stat { get; }

  public float Width { get; }

  public float Padding { get; }

  public TextAnchor HeaderAnchor { get; set; } = TextAnchor.UpperLeft;

  public TextAnchor Anchor { get; set; } = TextAnchor.UpperRight;

  public string Name
  {
    get
    {
      if (Type == ColumnType.Name)
        return "Method";
      return Stat switch
      {
        Stat.Total  => "Total",
        Stat.Mean   => "Mean",
        Stat.Median => "Median",
        Stat.StdDev => "Std Dev",
        Stat.None   => throw new ArgumentException(nameof(Stat)),
        _           => throw new NotImplementedException()
      };
    }
  }

  public enum ColumnType
  {
    Name,
    Stat,
  }
}