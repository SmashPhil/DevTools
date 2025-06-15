using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using Verse;

namespace DevTools.UnitTesting;

[PublicAPI]
public class TickObserver<T> : IDisposable where T : Entity
{
  private static readonly Dictionary<T, int> tickCounters = [];

  private readonly T entity;

  static TickObserver()
  {
    Harmony harmony = new($"TickObserver_{typeof(T).Name}");
    MethodInfo method = AccessTools.Method(typeof(T), "Tick");
    harmony.Patch(method, postfix: new HarmonyMethod(typeof(TickObserver<T>), nameof(RecordTick)));
  }

  public TickObserver(T entity)
  {
    this.entity = entity;
    tickCounters[entity] = 0;
  }

  public int TickCount => tickCounters.TryGetValue(entity);

  private static void RecordTick(T __instance)
  {
    if (tickCounters.ContainsKey(__instance))
    {
      tickCounters[__instance]++;
    }
  }

  void IDisposable.Dispose()
  {
    tickCounters.Remove(entity);
  }
}