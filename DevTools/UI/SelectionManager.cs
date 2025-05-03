using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Verse;

namespace DevTools;

public abstract class SelectionManager
{
  public event Action OnSelectionChanged;
  public readonly HashSet<ISelectable> selected = [];

  public bool AnySelected => selected.Count > 0;

  private static bool ShiftDown =>
    Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

  private static bool ControlDown
  {
    get
    {
      if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        return Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);
      return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
    }
  }

  protected virtual void ShowContextMenu()
  {
  }

  public bool IsSelected(ISelectable item)
  {
    return selected.Contains(item);
  }

  public void HandleClicks(Rect rect, ISelectable item)
  {
    if (Event.current is { type: EventType.MouseUp })
    {
      if (!Mouse.IsOver(rect))
      {
        return;
      }

      switch (Event.current.button)
      {
        case 0:
          if (ShiftDown)
          {
            selected.Add(item);
          }
          else if (ControlDown)
          {
            if (!selected.Add(item))
              selected.Remove(item);
          }
          else
          {
            Clear();
            selected.Add(item);
          }
          OnSelectionChanged?.Invoke();
          break;
        case 1:
          ShowContextMenu();
          break;
      }
      Event.current.Use();
    }
  }

  public void Clear()
  {
    selected.Clear();
  }
}