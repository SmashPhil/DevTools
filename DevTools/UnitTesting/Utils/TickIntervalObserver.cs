using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using Verse;

namespace DevTools.UnitTesting;

[PublicAPI]
public readonly struct TickIntervalObserver<T> : IDisposable where T : Entity
{
  private static readonly Dictionary<T, int> TickCounters = [];

  private readonly T entity;

  static TickIntervalObserver()
  {
    Harmony harmony = new($"TickIntervalObserver_{typeof(T).Name}");
    MethodInfo method = AccessTools.Method(typeof(T), "TickInterval");
    harmony.Patch(method,
      postfix: new HarmonyMethod(typeof(TickObserver<T>), nameof(RecordTickInterval)));
  }

  public TickIntervalObserver(T entity)
  {
    this.entity = entity;
    TickCounters[entity] = 0;
  }

  public int TickCount => TickCounters.TryGetValue(entity);

  private static void RecordTickInterval(T __instance)
  {
    if (TickCounters.ContainsKey(__instance))
    {
      TickCounters[__instance]++;
    }
  }

  void IDisposable.Dispose()
  {
    TickCounters.Remove(entity);
  }
}