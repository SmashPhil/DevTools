using System;
using JetBrains.Annotations;

namespace DevTools.Testing;

[PublicAPI, MeansImplicitUse]
[AttributeUsage(AttributeTargets.Parameter)]
public class DefParameterAttribute : Attribute
{
  public DefParameterAttribute()
  {
  }

  /// <summary>
  /// Defines a parameter list for all defs matching the parameter Def type.
  /// </summary>
  /// <param name="packageIds">
  /// Restrict the defs to those from mods matching these package ids.
  /// </param>
  public DefParameterAttribute(params string[] packageIds)
  {
    OnlyFromMods = packageIds;
  }
  
  internal string[] OnlyFromMods { get; }
}
