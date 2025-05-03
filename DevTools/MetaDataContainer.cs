using System;
using System.Collections.Generic;
using System.Reflection;

namespace DevTools;

internal class MetaDataContainer
{
  private readonly Dictionary<int, object> metaDataLookup = [];

  public T Get<T>(int key, T fallback = default)
  {
    return metaDataLookup.TryGetValue(key, out object value) ? (T)value : fallback;
  }

  public void Load(MemberInfo memberInfo)
  {
    foreach (Attribute attribute in memberInfo.GetCustomAttributes(true))
    {
      if (attribute is IMetaData metaData)
        metaDataLookup[metaData.Key] = metaData.Value;
    }
  }
}