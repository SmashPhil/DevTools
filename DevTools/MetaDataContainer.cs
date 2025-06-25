using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;

namespace DevTools;

[PublicAPI]
public class MetaDataContainer
{
  private readonly Dictionary<int, object> metaDataLookup = [];

  public T Get<T>(int key, T fallback = default)
  {
    return metaDataLookup.TryGetValue(key, out object value) ? (T)value : fallback;
  }

  public object GetRaw(int key)
  {
    return metaDataLookup.GetValueOrDefault(key);
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