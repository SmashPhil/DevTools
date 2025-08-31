using System;
using System.Reflection;
using HarmonyLib;

namespace DevTools.Testing;

public readonly struct ScopedMethodHook : IDisposable
{
  private static readonly Harmony Harmony = new("DevTools.TemporaryPatch");

  private readonly MethodInfo original;
  private readonly HarmonyMethod prefix;
  private readonly HarmonyMethod postfix;
  private readonly HarmonyMethod finalizer;

  public ScopedMethodHook(MethodInfo original, HarmonyMethod prefix = null, HarmonyMethod postfix = null,
    HarmonyMethod finalizer = null)
  {
    this.original = original;
    this.prefix = prefix;
    this.postfix = postfix;
    this.finalizer = finalizer;

    Harmony.Patch(original,
      prefix: prefix,
      postfix: postfix,
      finalizer: finalizer);
  }

  void IDisposable.Dispose()
  {
    if (prefix != null)
      Harmony.Unpatch(original, prefix.method);
    if (postfix != null)
      Harmony.Unpatch(original, postfix.method);
    if (finalizer != null)
      Harmony.Unpatch(original, finalizer.method);
  }
}