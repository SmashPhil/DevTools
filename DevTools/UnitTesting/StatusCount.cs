using System;
using System.Linq;
using UnityEngine;
using MethodType = DevTools.UnitTesting.UnitTestGroup.Method.MethodType;

namespace DevTools.UnitTesting;

internal class StatusCount
{
  public int AssertFailCount { get; internal set; }
  public int ExceptionCount { get; internal set; }

  private readonly int[,] counts = new int[Enum.GetValues(typeof(MethodType)).Length,
    Enum.GetValues(typeof(Status)).Length];

  public int this[MethodType type, Status status] => counts[(int)type, (int)status];

  public Vector2Int Dimension => new(counts.GetLength(0), counts.GetLength(1));

  public int Total
  {
    get
    {
      int total = 0;
      for (int i = 0; i < counts.GetLength(0); i++)
      {
        for (int j = 0; j < counts.GetLength(1); j++)
        {
          total += counts[i, j];
        }
      }
      return total;
    }
  }

  public void Increment(MethodType type, Status status)
  {
    counts[(int)type, (int)status]++;
  }

  public void Join(StatusCount other)
  {
    for (int i = 0; i < counts.GetLength(0); i++)
    {
      for (int j = 0; j < counts.GetLength(1); j++)
      {
        counts[i, j] += other.counts[i, j];
      }
    }
  }

  public override string ToString()
  {
    return string.Join("\n\n",
      Enum.GetValues(typeof(MethodType))
       .Cast<MethodType>()
       .Select(type =>
          $"{type}:\n" + string.Join("\n",
            Enum.GetValues(typeof(Status))
             .Cast<Status>()
             .Select(status =>
                $"    {counts[(int)type, (int)status]} {status}"
              )
          )
        )
    );
  }
}