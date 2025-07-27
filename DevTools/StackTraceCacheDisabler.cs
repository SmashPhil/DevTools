using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine.Assertions;
using Verse;

namespace DevTools;

public readonly struct StackTraceCacheDisabler : IDisposable
{
  private static readonly FieldInfo NoStacktraceCaching;

  private readonly bool oldValue;

  static StackTraceCacheDisabler()
  {
    Type harmonyMain = GenTypes.GetTypeInAnyAssembly("HarmonyMod.HarmonyMain");
    Assert.IsNotNull(harmonyMain);
    NoStacktraceCaching = AccessTools.Field(harmonyMain, "noStacktraceCaching");
    Assert.IsNotNull(NoStacktraceCaching);
  }

  public StackTraceCacheDisabler()
  {
    oldValue = (bool)NoStacktraceCaching.GetValue(null);
    NoStacktraceCaching.SetValue(null, true);
  }

  public void Dispose()
  {
    NoStacktraceCaching.SetValue(null, oldValue);
  }
}