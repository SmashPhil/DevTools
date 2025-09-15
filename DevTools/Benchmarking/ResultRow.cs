using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace DevTools.Benchmarking;

internal class ResultRow : IDataRow<ResultColumn>
{
	public ResultRow(string name, Benchmark.Result result)
	{
		Name = name;
		Result = result;
	}

	private string Name { get; }

	private Benchmark.Result Result { get; }

	bool IDataRow<ResultColumn>.ShouldHide => false;

	bool IDataRow<ResultColumn>.CanExpand => false;

	bool IDataRow<ResultColumn>.Expanded { get; set; }

	float IDataRow<ResultColumn>.Height => Text.LineHeightOf(GameFont.Small);

	IEnumerable<IDataRow<ResultColumn>> IDataRow<ResultColumn>.NestedRows
	{
		get { yield break; }
	}

	void IDataRow<ResultColumn>.Draw(Rect rect, ResultColumn column)
	{
		if (column.Type == ResultColumn.ColumnType.Name)
		{
			Widgets.Label(rect, Name);
			return;
		}
		switch (column.Stat)
		{
			case Stat.Total:
				Widgets.Label(rect, $"{Result.Formatted(Result.Total)}");
			break;
			case Stat.Mean:
				Widgets.Label(rect, $"{Result.Formatted(Result.Mean)}");
			break;
			case Stat.Median:
				Widgets.Label(rect, $"{Result.Formatted(Result.Median)}");
			break;
			case Stat.StdDev:
				Widgets.Label(rect, $"{Result.Formatted(Result.StdDev)}");
			break;
			case Stat.Samples:
				Widgets.Label(rect, $"{Result.Samples}");
			break;
			case Stat.Partitions:
				Widgets.Label(rect, $"{Result.Partitions}");
			break;
			case Stat.None:
				throw new ArgumentException(nameof(Stat));
			default:
				throw new NotImplementedException(nameof(Stat));
		}
	}
}