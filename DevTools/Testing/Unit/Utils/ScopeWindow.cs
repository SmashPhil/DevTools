using System;
using JetBrains.Annotations;
using Verse;

namespace DevTools.Testing;

/// <summary>
/// Captures a Window reference and closes it when this object goes out of scope.
/// </summary>
[PublicAPI]
public readonly struct ScopeWindow : IDisposable
{
  private readonly Window window;

  public ScopeWindow(Window window)
  {
    this.window = window;
  }

  void IDisposable.Dispose()
  {
    window.Close(doCloseSound: false);
  }
}