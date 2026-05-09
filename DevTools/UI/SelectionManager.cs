using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using Verse;

namespace DevTools;

public abstract class SelectionManager<T> where T : ISelectable
{
  public event Action OnSelectionChanged;
  public readonly HashSet<T> selected = [];

  public bool AnySelected => selected.Count > 0;

  protected abstract IEnumerable<T> AllItems { get; }

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

  public bool IsSelected(T item)
  {
    return selected.Contains(item);
  }

  public void MouseEventArea(Rect rect, T item)
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
            if (AnySelected)
            {
              T first = AllItems.First(it => selected.Contains(it));
              selected.Clear();
              selected.Add(first);
              foreach (T eligible in AllItems.Between(first, item))
              {
                selected.Add(eligible);
              }
            }
            else
            {
              selected.Add(item);
            }
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