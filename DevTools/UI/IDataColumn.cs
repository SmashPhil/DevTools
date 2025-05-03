using UnityEngine;

namespace DevTools;

public interface IDataColumn
{
  string Name { get; }
  float Width { get; }
  TextAnchor HeaderAnchor { get; }
  TextAnchor Anchor { get; }
}