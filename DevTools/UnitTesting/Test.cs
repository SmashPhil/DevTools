using System;
using System.Collections.Generic;

namespace DevTools.UnitTesting;

public static class Test
{
  private static readonly Stack<string> subCategories = [];

  public static void BeginGroup(string label)
  {
  }

  public static void EndGroup(string label)
  {
  }

  public static void Cancel(string reason)
  {
    Expect.Signal(reason, Status.Canceled);
  }

  public static void Skip(string reason)
  {
    Expect.Signal(reason, Status.Skipped);
  }

  public readonly struct Group : IDisposable
  {
    private readonly string label;

    public Group(string label)
    {
      BeginGroup(label);
    }

    public void Dispose()
    {
      EndGroup(label);
    }
  }
}