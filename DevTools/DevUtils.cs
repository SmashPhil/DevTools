using System;
using System.Collections.Generic;
using System.Linq;

namespace DevTools;

internal static class DevUtils
{
  extension<T>(IEnumerable<T> source)
  {
    public IEnumerable<T> Between(T a, T b)
    {
      var list = source as IList<T> ?? source.ToList();

      int indexA = list.IndexOf(a);
      int indexB = list.IndexOf(b);

      if (indexA == -1 || indexB == -1)
        yield break;

      int start = Math.Min(indexA, indexB);
      int end = Math.Max(indexA, indexB);

      for (int i = start; i <= end; i++)
      {
        yield return list[i];
      }
    }
  }
}
