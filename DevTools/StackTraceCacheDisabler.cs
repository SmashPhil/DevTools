using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine.Assertions;
using Verse;

namespace DevTools;

public readonly struct StackTraceCacheDisabler : IDisposable
{
  private static readonly FieldInfo noStacktraceCaching;

  private readonly bool oldValue;

  static StackTraceCacheDisabler()
  {
    Type harmonyMain = GenTypes.GetTypeInAnyAssembly("HarmonyMod.HarmonyMain");
    Assert.IsNotNull(harmonyMain);
    noStacktraceCaching = AccessTools.Field(harmonyMain, "noStacktraceCaching");
    Assert.IsNotNull(noStacktraceCaching);
  }

  public StackTraceCacheDisabler()
  {
    oldValue = (bool)noStacktraceCaching.GetValue(null);
    noStacktraceCaching.SetValue(null, true);
  }

  public void Dispose()
  {
    noStacktraceCaching.SetValue(null, oldValue);
  }
}