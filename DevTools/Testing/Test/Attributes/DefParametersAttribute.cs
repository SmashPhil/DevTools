using System;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;

namespace DevTools.Testing;

/// <summary>
/// Provides matching defs as parameter values.
/// </summary>
[PublicAPI, MeansImplicitUse]
[AttributeUsage(AttributeTargets.Parameter)]
public class DefParametersAttribute : Attribute
{
  /// <summary>
  /// Defines a parameter list for all defs matching the parameter Def type.
  /// </summary>
  /// <param name="packageIds">
  /// Restrict the defs to those from mods matching these package ids.
  /// </param>
  public DefParametersAttribute(params string[] packageIds)
  {
    OnlyFromMods = packageIds;
  }

  /// <summary>
  /// Defines a filtered parameter list for all defs matching the parameter Def type.
  /// </summary>
  /// <param name="type">Declaring type of the filter function.</param>
  /// <param name="filterName">Name of the filter function.</param>
  /// <param name="packageIds">
  /// Restrict the defs to those from mods matching these package ids.
  /// </param>
  public DefParametersAttribute(Type type, string filterName, params string[] packageIds)
  {
    OnlyFromMods = packageIds;
    Filter = AccessTools.Method(type, filterName);
  }

  /// <summary>
  /// Package ids allowed to provide defs.
  /// </summary>
  internal string[] OnlyFromMods { get; }

  /// <summary>
  /// Optional method used to filter defs.
  /// </summary>
  internal MethodInfo Filter { get; }
}
