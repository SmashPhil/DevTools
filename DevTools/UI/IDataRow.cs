using System.Collections.Generic;
using UnityEngine;

namespace DevTools;

public interface IDataRow<in C> : ISelectable where C : class, IDataColumn
{
  bool CanExpand { get; }
  bool Expanded { get; set; }
  float Height { get; }
  IEnumerable<IDataRow<C>> NestedRows { get; }
  void Draw(Rect rect, C column);
}