using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace DevTools;

[UsedImplicitly(ImplicitUseTargetFlags.Members)]
public class DataTable<C, R> where C : class, IDataColumn
                             where R : class, IDataRow<C>

{
  private const float SeparatorPadding = 25;
  private const float SeparatorSize = 1;
  private const float PadLeftRight = (SeparatorPadding - SeparatorSize) / 2f;

  private readonly Color backgroundLightColor = new(0.25f, 0.25f, 0.25f, 0.25f);

  private readonly List<C> columns = [];
  private readonly List<R> rows = [];

  private Vector2 scrollPos;

  public GameFont HeaderFont { get; set; } = GameFont.Small;

  public GameFont DataFont { get; set; } = GameFont.Small;

  /// <summary>
  /// Nested IDataRow entries will indent the first column
  /// </summary>
  public float Indent { get; set; } = 15;

  public bool CanExpandItems { get; set; } = true;

  private float Height { get; set; } = -1;

  public SelectionManager<R> Selector { get; private set; }

  public void RecacheHeight()
  {
    using TextBlock tb = new(DataFont);
    Height = GetHeightRecursive(rows);
    return;

    static float GetHeightRecursive(IEnumerable<R> rows)
    {
      float height = 0;
      foreach (R row in rows)
      {
        if (row.ShouldHide)
          continue;
        height += row.Height;
        if (row is { CanExpand: true, Expanded: true })
        {
          height += GetHeightRecursive(row.NestedRows.Cast<R>());
        }
      }
      return height;
    }
  }

  public void SetSelector(SelectionManager<R> selectionManager)
  {
    Selector = selectionManager;
  }

  public void SetRows(IEnumerable<R> rows)
  {
    this.rows.Clear();
    this.rows.AddRange(rows);
  }

  public void SetColumns(params C[] columns)
  {
    this.columns.Clear();
    this.columns.AddRange(columns);
  }

  public void AddColumn(C column)
  {
    this.columns.Add(column);
  }

  public void DrawTable(Rect inRect)
  {
    if (Height < 0)
      RecacheHeight();

    Widgets.BeginGroup(inRect);
    float curX = 0;
    Rect headerRect;
    using (new TextBlock(HeaderFont))
    {
      headerRect = (inRect with { height = Text.LineHeight }).AtZero();
      foreach (C column in columns)
      {
        using TextBlock alignmentBlock = new(column.HeaderAnchor);
        headerRect = headerRect with { x = curX, width = column.Width };
        Widgets.Label(headerRect.ContractedBy(PadLeftRight, 0), column.Name);
        WidgetUtils.VerticalSeparator(headerRect.xMax, headerRect.yMin, headerRect.height,
          size: SeparatorSize);
        curX = headerRect.xMax;
      }
    }
    Widgets.EndGroup();

    float listerY = inRect.y + headerRect.height;
    WidgetUtils.HorizontalSeparator(inRect.x, listerY, inRect.width);

    Rect outRect = inRect with { yMin = listerY + 1 };
    Rect viewRect = outRect.AtZero() with { width = outRect.width - 16, height = Height };
    Widgets.BeginScrollView(outRect, ref scrollPos, viewRect);
    using (new TextBlock(DataFont))
    {
      float curY = 0;
      curX = 0;
      DrawRows(viewRect, ref curX, ref curY, rows, columns);
    }
    Widgets.EndScrollView();

    // If click event hasn't been used by this point, clear selection
    if (Event.current.type == EventType.MouseUp)
      Selector?.Clear();
  }

  private void DrawRows(Rect viewRect, ref float curX, ref float curY,
    IEnumerable<R> rows, IEnumerable<C> columns)
  {
    // ReSharper disable PossibleMultipleEnumeration

    const float ExpandBtnSize = 20;

    foreach (R row in rows)
    {
      if (row.ShouldHide)
        continue;
      Rect expandBtnRect =
        new Rect(curX, curY, row.Height, row.Height).ContractedBy((row.Height - ExpandBtnSize) / 2);
      Rect rowRect = new(0, curY, viewRect.width, row.Height);
      float cellX = 0;
      float indent = CanExpandItems ? expandBtnRect.xMax : curX;
      foreach (C column in columns)
      {
        using TextBlock alignmentBlock = new(column.Anchor);
        Rect cellRect = rowRect with { x = cellX, width = column.Width };
        if (indent > 0)
          cellRect.xMin += indent;
        row.Draw(cellRect.ContractedBy(PadLeftRight, 0), column);
        cellX = cellRect.xMax;
        indent = 0; // Quickest way to remove indent beyond first column
      }

      curY += row.Height;

      if (CanExpandItems && row.CanExpand)
      {
        bool expanded = row.Expanded;
        if (CollapseButton(expandBtnRect, ref expanded))
        {
          row.Expanded = expanded;
          if (expanded)
            SoundDefOf.TabOpen.PlayOneShotOnCamera();
          else
            SoundDefOf.TabClose.PlayOneShotOnCamera();
          RecacheHeight();
        }
        if (expanded)
        {
          curX += Indent;
          DrawRows(viewRect, ref curX, ref curY, row.NestedRows.Cast<R>(), columns);
          curX -= Indent;
        }
      }
      Selector?.HandleClicks(rowRect, row);
      if (Selector != null && Selector.IsSelected(row))
        Widgets.DrawBoxSolid(rowRect, backgroundLightColor);
    }
  }

  private static bool CollapseButton(Rect rect, ref bool expanded, bool doMouseoverSound = true,
    string tooltip = null)
  {
    bool result = Widgets.ButtonImage(rect, expanded ? TexButton.Collapse : TexButton.Reveal,
      baseColor: Color.white, mouseoverColor: GenUI.MouseoverColor,
      doMouseoverSound: doMouseoverSound,
      tooltip: tooltip);
    if (result)
      expanded = !expanded;
    return result;
  }
}