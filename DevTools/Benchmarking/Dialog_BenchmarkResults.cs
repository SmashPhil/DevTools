using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace DevTools.Benchmarking;

internal class Dialog_BenchmarkResults : Window
{
  public override Vector2 InitialSize => new(900, 500);

  private readonly string category;
  private readonly Graph graph;
  private readonly List<(string, Benchmark.Result)> results;

  private readonly DataTable<ResultColumn, ResultRow> table;

  private Dialog_BenchmarkResults(string category, List<(string, Benchmark.Result)> results)
  {
    this.category = category;
    this.results = results;
    SetProperties();
  }

  public Dialog_BenchmarkResults(Graph graph, string category,
    List<(string name, Benchmark.Result results)> results) :
    this(category, results)
  {
    this.graph = graph;
  }

  public Dialog_BenchmarkResults(Stat stats, string category,
    List<(string name, Benchmark.Result result)> results) :
    this(category, results)
  {
    const TextAnchor StatHeaderAnchor = TextAnchor.UpperRight;
    const TextAnchor StatTextAnchor = TextAnchor.MiddleRight;

    this.table = new DataTable<ResultColumn, ResultRow>
    {
      HeaderFont = GameFont.Medium
    };

    // Columns
    table.AddColumn(new ResultColumn(ResultColumn.ColumnType.Name, Stat.None, 200)
    {
      HeaderAnchor = TextAnchor.UpperRight,
      Anchor = TextAnchor.MiddleRight
    });
    if (stats.HasFlag(Stat.Total))
      table.AddColumn(new ResultColumn(ResultColumn.ColumnType.Stat, Stat.Total, 120)
      {
        HeaderAnchor = StatHeaderAnchor,
        Anchor = StatTextAnchor
      });
    if (stats.HasFlag(Stat.Mean))
      table.AddColumn(new ResultColumn(ResultColumn.ColumnType.Stat, Stat.Mean, 120)
      {
        HeaderAnchor = StatHeaderAnchor,
        Anchor = StatTextAnchor
      });
    if (stats.HasFlag(Stat.Median))
      table.AddColumn(new ResultColumn(ResultColumn.ColumnType.Stat, Stat.Median, 120)
      {
        HeaderAnchor = StatHeaderAnchor,
        Anchor = StatTextAnchor
      });
    if (stats.HasFlag(Stat.StdDev))
      table.AddColumn(new ResultColumn(ResultColumn.ColumnType.Stat, Stat.StdDev, 200)
      {
        HeaderAnchor = StatHeaderAnchor,
        Anchor = StatTextAnchor
      });

    // Rows
    List<ResultRow> rows = [];
    foreach ((string name, Benchmark.Result result) in results)
    {
      rows.Add(new ResultRow(name, result));
    }
    table.SetRows(rows);
  }

  public override bool IsDebug => true;

  private void SetProperties()
  {
    this.resizeable = true;
    this.closeOnCancel = true;
    this.doCloseX = true;
    this.forcePause = true;
    this.onlyDrawInDevMode = true;
    this.draggable = true;
  }

  public override void DoWindowContents(Rect inRect)
  {
    using (new TextBlock(GameFont.Medium))
    {
      Rect labelRect = inRect with { height = Text.LineHeight };
      Widgets.Label(labelRect, category);
      inRect.yMin = labelRect.yMax;
    }
    if (table is not null)
    {
      table.DrawTable(inRect);
      return;
    }
    if (graph is not null)
    {
      graph.Draw(inRect);
      return;
    }
    Log.ErrorOnce("No Draw function passed to Dialog_BenchmarkResults.", this.GetHashCode());
  }
}