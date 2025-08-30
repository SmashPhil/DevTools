using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using Verse;

namespace DevTools.UnitTesting;

/// <summary>
/// Observes the number of times <c>TickInterval</c> is called on a specific <typeparamref name="T"/> entity,
/// recording the count for test assertions. When disposed, the observation is removed.
/// </summary>
/// <typeparam name="T">The type of entity whose <c>TickInterval</c> calls will be recorded.</typeparam>
[PublicAPI]
public readonly struct TickIntervalObserver<T> : IDisposable where T : Entity
{
  /// <summary>
  /// Static mapping from observed entities to their recorded tick counts.
  /// </summary>
  private static readonly Dictionary<T, int> TickCounters = [];

  /// <summary>
  /// The target entity being observed.
  /// </summary>
  private readonly T entity;

  static TickIntervalObserver()
  {
    Harmony harmony = new($"TickIntervalObserver_{typeof(T).Name}");
    MethodInfo method = AccessTools.Method(typeof(T), "TickInterval");
    harmony.Patch(method,
      postfix: new HarmonyMethod(typeof(TickObserver<T>), nameof(RecordTickInterval)));
  }

  /// <summary>
  /// Initializes a new observer for the specified entity, resetting its count to zero.
  /// </summary>
  /// <param name="entity">The entity whose tick interval calls will be counted.</param>
  public TickIntervalObserver(T entity)
  {
    this.entity = entity;
    TickCounters[entity] = 0;
  }

  /// <summary>
  /// Gets the number of <c>TickInterval</c> calls recorded for the observed entity.
  /// </summary>
  public int TickCount => TickCounters.TryGetValue(entity);

  /// <summary>
  /// Harmony postfix callback that increments the tick counter for the patched entity.
  /// </summary>
  /// <param name="__instance">The entity instance whose <c>TickInterval</c> was invoked.</param>
  private static void RecordTickInterval(T __instance)
  {
    if (TickCounters.ContainsKey(__instance))
    {
      TickCounters[__instance]++;
    }
  }

  /// <summary>
  /// Stops observing the entity and removes its recorded tick count.
  /// </summary>
  void IDisposable.Dispose()
  {
    TickCounters.Remove(entity);
  }
}