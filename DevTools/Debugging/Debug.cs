using UnityEngine;
using Verse;

namespace DevTools;

public static class Debug
{
  private static readonly Vector2 popupSize = new(500, 650);

  internal static void ShowStack(string label, string message)
  {
    // Extract stack trace before potentially sending it off to UnityThread
    // where the stack trace will be completely different.
    StackTracePopup popup = new(popupSize, label, message);

    if (!UnityData.IsInMainThread)
    {
      popup.SendToLog();
      // WindowStack is not thread safe, we'll need to hand it off to the
      // UnityThread to invoke on the main thread.
      //UnityThread.ExecuteOnMainThread(() => Find.WindowStack.Add(popup));
      return;
    }
    Find.WindowStack.Add(popup);
  }
}