using System;
using JetBrains.Annotations;

namespace DevTools.Testing;

[PublicAPI]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class LoadIfModsActiveAttribute : Attribute
{
  public LoadIfModsActiveAttribute(params string[] packageIds)
  {
    PackageIds = packageIds;
  }

  public string[] PackageIds { get; }
}