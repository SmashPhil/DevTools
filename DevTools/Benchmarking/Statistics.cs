using System;
using System.Linq;

namespace DevTools.Benchmarking;

internal static class Statistics
{
  public static long MedianLong(this long[] array)
  {
    if (array.Length == 0)
      return 0;
    if (array.Length == 1)
      return array[0];

    Array.Sort(array);
    int n = array.Length;
    if (n % 2 == 0)
      return array[(n + 1) / 2 - 1];
    return (array[n / 2 - 1] + array[n / 2]) / 2;
  }

  public static double Median(this long[] array)
  {
    if (array.Length == 0)
      return 0;
    if (array.Length == 1)
      return array[0];

    Array.Sort(array);
    int n = array.Length;
    if (n % 2 == 0)
      return array[(n + 1) / 2 - 1];
    return (array[n / 2 - 1] + array[n / 2]) / 2.0d;
  }

  public static double Mean(this long[] array)
  {
    if (array.Length == 0)
      return 0;
    if (array.Length == 1)
      return array[0];

    return array.Length > 0 ? array.Average() : 0;
  }

  public static double StdDev(this long[] array)
  {
    if (array.Length == 0)
      return 0;
    if (array.Length == 1)
      return 0;

    double mean = array.Mean();
    double stdDev = Math.Sqrt(array.Sum(value => (value - mean) * (value - mean) / array.Length));
    return stdDev;
  }
}