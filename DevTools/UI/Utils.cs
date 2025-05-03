using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace DevTools;

public static class Utils
{
  private static readonly Color separatorColor = new ColorInt(135, 135, 135).ToColor;

  public static void HorizontalSeparator(float x, float y, float length, float size = 1)
  {
    using TextBlock lineColor = new(separatorColor);
    GUI.DrawTexture(new Rect(x, y, length, size), BaseContent.WhiteTex);
  }

  public static void VerticalSeparator(float x, float y, float length, float size = 1)
  {
    using TextBlock lineColor = new(separatorColor);
    GUI.DrawTexture(new Rect(x, y, size, length), BaseContent.WhiteTex);
  }

  public static Rect ClipInsideWindow(this Rect rect)
  {
    if (rect.x < 0)
      rect.x = 0;
    if (rect.xMax > UI.screenWidth)
      rect.x = UI.screenWidth - rect.width;
    if (rect.y < 0)
      rect.y = 0;
    if (rect.yMax > UI.screenHeight)
      rect.y = UI.screenHeight - rect.height;
    if (rect.width > UI.screenWidth)
      rect.width = UI.screenWidth;
    if (rect.height > UI.screenHeight)
      rect.height = UI.screenHeight;
    return rect;
  }

  public static void DoRecursive<T>(T root, Action<T> action, Func<T, IEnumerable<T>> children)
  {
    Stack<T> stack = new();
    stack.Push(root);
    while (stack.Count > 0)
    {
      T current = stack.Pop();
      action(current);

      foreach (T child in children(current))
      {
        stack.Push(child);
      }
    }
  }
}