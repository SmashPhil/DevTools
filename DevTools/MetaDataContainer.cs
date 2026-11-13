using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;

namespace DevTools;

[PublicAPI]
public class MetaDataContainer
{
  private readonly Dictionary<string, List<object>> metaDataLookup = [];

  public T Get<T>(string key, T fallback = default)
  {
    return metaDataLookup.TryGetValue(key, out List<object> values) ? (T)values[^1] : fallback;
  }

  public object GetRaw(string key)
  {
    return metaDataLookup.TryGetValue(key, out List<object> values) ? values[^1] : null;
  }

  public IEnumerable<T> GetAll<T>(string key)
  {
    return metaDataLookup.TryGetValue(key, out List<object> values) ? values.Select(value => (T)value) : [];
  }

  public void Load(MemberInfo memberInfo)
  {
    foreach (Attribute attribute in memberInfo.GetCustomAttributes(true))
    {
      if (attribute is not IMetaData metaData)
        continue;

      if (!metaDataLookup.TryGetValue(metaData.Key, out List<object> values))
        metaDataLookup[metaData.Key] = values = [];
      values.Add(metaData.Value);
    }
  }
}