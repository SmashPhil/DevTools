using System;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;

namespace DevTools.Testing;

/// <summary>
/// Provides matching defs as parameter values.
/// </summary>
[Obsolete("Use DefSourceAttribute instead.", error: true), MeansImplicitUse]
[AttributeUsage(AttributeTargets.Parameter)]
public class DefParametersAttribute : DefsSourceAttribute
{
  /// <summary>
  /// Defines a parameter list for all defs matching the parameter Def type.
  /// </summary>
  /// <param name="packageIds">
  /// Restrict the defs to those from mods matching these package ids.
  /// </param>
  public DefParametersAttribute(params string[] packageIds) : base(packageIds)
  {
  }

  /// <summary>
  /// Defines a filtered parameter list for all defs matching the parameter Def type.
  /// </summary>
  /// <param name="type">Declaring type of the filter function.</param>
  /// <param name="filterName">Name of the filter function.</param>
  /// <param name="packageIds">
  /// Restrict the defs to those from mods matching these package ids.
  /// </param>
  public DefParametersAttribute(Type type, string filterName, params string[] packageIds) : base(type, filterName, packageIds)
  {
  }
}
