using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using Verse;

namespace DevTools.UnitTesting;

/// <summary>
/// Observes and counts the number of times <c>Tick</c> is called on a specific <typeparamref name="T"/> entity.
/// </summary>
/// <typeparam name="T">The entity type whose <c>Tick</c> invocations will be recorded.</typeparam>
[PublicAPI]
public readonly struct TickObserver<T> : IDisposable where T : Entity
{
  /// <summary>
  /// Static storage mapping observed entities to their tick-invocation counts.
  /// </summary>
  private static readonly Dictionary<T, int> TickCounters = [];

  /// <summary>
  /// The target entity being observed.
  /// </summary>
  private readonly T entity;

  static TickObserver()
  {
    Harmony harmony = new($"TickObserver_{typeof(T).Name}");
    MethodInfo method = AccessTools.Method(typeof(T), "Tick");
    harmony.Patch(method, postfix: new HarmonyMethod(typeof(TickObserver<T>), nameof(RecordTick)));
  }

  /// <summary>
  /// Records the tick count for <paramref name="entity"/>.
  /// </summary>
  /// <param name="entity">The entity instance whose ticks are to be counted.</param>
  public TickObserver(T entity)
  {
    this.entity = entity;
    TickCounters[entity] = 0;
  }

  /// <summary>
  /// Gets the number of <c>Tick</c> calls recorded for the observed entity.
  /// </summary>
  public int TickCount => TickCounters.TryGetValue(entity);

  /// <summary>
  /// Harmony postfix callback invoked after <c>Tick</c> is called on an entity, incrementing its counter.
  /// </summary>
  /// <param name="__instance">The entity instance whose <c>Tick</c> was invoked.</param>
  private static void RecordTick(T __instance)
  {
    if (TickCounters.ContainsKey(__instance))
    {
      TickCounters[__instance]++;
    }
  }

  /// <summary>
  /// Disposes the observer, removing the entity from the recorded tick counters.
  /// </summary>
  void IDisposable.Dispose()
  {
    TickCounters.Remove(entity);
  }
}