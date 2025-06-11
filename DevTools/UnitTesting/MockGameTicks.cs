using System;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine.Assertions;
using Verse;

namespace DevTools.UnitTesting;

[PublicAPI]
public readonly struct MockGameTicks : IDisposable
{
  private static FieldInfo ticksGameField;

  private readonly TickManager tickManager;
  private readonly int ticksGame;

  static MockGameTicks()
  {
    ticksGameField = AccessTools.Field(typeof(TickManager), "ticksGameInt");
    Assert.IsNotNull(ticksGameField);
  }

  public MockGameTicks(int ticksGame)
  {
    tickManager = Find.TickManager;
    this.ticksGame = Find.TickManager.TicksGame;
    ticksGameField.SetValue(tickManager, ticksGame);
  }

  void IDisposable.Dispose()
  {
    ticksGameField.SetValue(tickManager, ticksGame);
  }
}