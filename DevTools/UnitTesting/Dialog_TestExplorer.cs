using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using Method = DevTools.UnitTesting.UnitTestGroup.Method;

namespace DevTools.UnitTesting;

internal class Dialog_TestExplorer : Window
{
  private const float LineHeight = 30;
  private const float HeaderPadding = 30;
  private const float ResultIconSize = 24;
  private const float ExpandBtnSize = 20;
  private const float TabSize = ExpandBtnSize + ResultIconSize / 2;
  private const float TopHeaderHeight = 90;

  private static readonly Color inactiveColor = new(0.37f, 0.37f, 0.37f, 0.8f);
  private readonly Color backgroundLightColor = new(0.25f, 0.25f, 0.25f, 0.25f);

  private readonly UnitTestManager unitTestManager;
  private readonly List<UnitTestGroup> testGroups;
  private readonly bool[] expanded;
  private readonly SelectionManager selector;

  private Vector2 scrollPos;

  public Dialog_TestExplorer(UnitTestManager unitTestManager, List<UnitTestGroup> testGroups)
  {
    this.unitTestManager = unitTestManager;
    this.selector = new SelectionManager(this.unitTestManager);
    this.testGroups = testGroups;
    expanded = new bool[testGroups.Count];
    SetWindowProperties();
  }

  private float Height { get; set; }

  public override Vector2 InitialSize => new(UI.screenWidth, UI.screenHeight);

  public override bool IsDebug => true;

  private void SetWindowProperties()
  {
    layer = WindowLayer.Super;
    closeOnAccept = false;
    preventDrawTutor = true;
    onlyDrawInDevMode = true;
  }

  public override void PreOpen()
  {
    base.PreOpen();
    RecacheHeight();
  }

  private void RecacheHeight()
  {
    using TextBlock tb = new(GameFont.Small);
    Height = 0;
    for (int i = 0; i < testGroups.Count; i++)
    {
      Height += LineHeight;
      if (expanded[i])
      {
        Height += LineHeight * testGroups[i].tests.Count;
      }
    }
  }

  public override void DoWindowContents(Rect inRect)
  {
    using TextBlock resultText = new(GameFont.Small, TextAnchor.MiddleLeft);

    Rect headerRect = inRect with { height = LineHeight };
    using (new TextBlock(GameFont.Small, TextAnchor.UpperLeft))
    {
      Widgets.Label(headerRect, "Test Explorer");
      headerRect.yMin += LineHeight;
    }

    DrawHeaderButtons(headerRect);

    //Widgets.Label(headerRect, "0 Warnings 0 Errors");
    headerRect.yMin += LineHeight;
    Widgets.DrawLineHorizontal(headerRect.x, headerRect.y, headerRect.width);

    Rect outRect = inRect with { yMin = headerRect.yMin + 5 };
    Rect viewRect = outRect.AtZero() with { width = outRect.width - 16, height = Height };
    Widgets.BeginScrollView(outRect, ref scrollPos, viewRect);
    float curY = 0;
    for (int i = 0; i < testGroups.Count; i++)
    {
      UnitTestGroup testGroup = testGroups[i];

      Rect rect = new(viewRect.x, curY, viewRect.width, LineHeight);
      Rect expandBtnRect = (rect with { size = new Vector2(LineHeight, LineHeight) })
       .ContractedBy((LineHeight - ExpandBtnSize) / 2);
      Rect testChkRect = (expandBtnRect with
      {
        x = expandBtnRect.xMax,
        size = new Vector2(LineHeight, LineHeight)
      }).ContractedBy((LineHeight - ResultIconSize) / 2);
      Rect labelRect = rect with
      {
        x = testChkRect.xMax,
        width = rect.width - testChkRect.width - expandBtnRect.width
      };
      labelRect.xMin += 5; // +5 to pad a bit between the label and result tick mark

      bool expand = expanded[i];
      if (CollapseButton(expandBtnRect, ref expand))
      {
        expanded[i] = expand;
        if (expand)
          SoundDefOf.TabOpen.PlayOneShotOnCamera();
        else
          SoundDefOf.TabClose.PlayOneShotOnCamera();
        RecacheHeight();
      }
      CheckboxDraw(testChkRect, testGroup.Result == Status.Passed, false);
      Widgets.Label(labelRect, testGroup.Category);
      selector.HandleClicks(labelRect, testGroup);
      if (selector.IsSelected(testGroup))
        Widgets.DrawBoxSolid(labelRect, backgroundLightColor);

      if (expand)
      {
        expandBtnRect.x += TabSize;
        testChkRect.x += TabSize;
        labelRect.x += TabSize;
        foreach (Method method in testGroup.tests)
        {
          curY += LineHeight;
          testChkRect.y = curY;
          labelRect.y = curY;
          CheckboxDraw(testChkRect, method.Status == Status.Passed, false);
          Widgets.Label(labelRect, method.Name);
          selector.HandleClicks(labelRect, method);
          if (selector.IsSelected(method))
            Widgets.DrawBoxSolid(labelRect, backgroundLightColor);
        }
      }
      curY += LineHeight;
    }
    Widgets.EndScrollView();
  }

  private void DrawHeaderButtons(Rect rect)
  {
    Rect buttonRect = (rect with { size = new Vector2(LineHeight, LineHeight) }).ContractedBy(3);
    if (Widgets.ButtonImage(buttonRect, TexButton.SpeedButtonTextures[2], Color.green,
      tooltip: "Run Plan"))
    {
      SoundDefOf.Click.PlayOneShotOnCamera();
      if (!unitTestManager.TestPlans.NullOrEmpty())
      {
        List<FloatMenuOption> options = [];
        foreach (TestPlan testPlan in unitTestManager.TestPlans)
        {
          options.Add(new FloatMenuOption(testPlan.name,
            delegate { unitTestManager.RunPlan(testPlan); }));
        }
        Find.WindowStack.Add(new FloatMenu(options));
      }
    }
    buttonRect.x += buttonRect.width;
    if (Widgets.ButtonImage(buttonRect, TexButton.SpeedButtonTextures[1], Color.green,
      tooltip: "Run"))
    {
      SoundDefOf.Click.PlayOneShotOnCamera();
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

  private static void CheckboxDraw(Rect rect, bool active, bool disabled,
    Texture2D texChecked = null, Texture2D texUnchecked = null)
  {
    if (disabled)
      GUI.color = inactiveColor;

    Texture2D image;
    if (active)
      image = texChecked != null ? texChecked : Widgets.CheckboxOnTex;
    else
      image = texUnchecked != null ? texUnchecked : Widgets.CheckboxOffTex;

    GUI.DrawTexture(rect, image);

    GUI.color = Color.white;
  }

  private class SelectionManager
  {
    private readonly UnitTestManager unitTestManager;

    private readonly HashSet<UnitTestGroup> selectedGroups = [];
    private readonly HashSet<Method> selectedMethods = [];

    public SelectionManager(UnitTestManager unitTestManager)
    {
      this.unitTestManager = unitTestManager;
    }

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

    public bool IsSelected<T>(T item)
    {
      return item switch
      {
        UnitTestGroup testGroup => selectedGroups.Contains(testGroup),
        Method method           => selectedMethods.Contains(method),
        _                       => throw new NotImplementedException(typeof(T).Name),
      };
    }

    public void HandleClicks(Rect rect, UnitTestGroup testGroup)
    {
      if (Event.current is { type: EventType.MouseUp })
      {
        switch (Event.current.button)
        {
          case 0:
            if (Mouse.IsOver(rect))
              ProcessSelect(selectedGroups, testGroup);
            else
              selectedGroups.Clear();
            break;
          case 1:
            ShowContextMenu();
            break;
        }
        Event.current.Use();
      }
    }

    public void HandleClicks(Rect rect, Method method)
    {
      if (Event.current is { type: EventType.MouseUp })
      {
        switch (Event.current.button)
        {
          case 0:
            if (Mouse.IsOver(rect))
              ProcessSelect(selectedMethods, method);
            else
              selectedMethods.Clear();
            break;
          case 1:
            ShowContextMenu();
            break;
        }
        Event.current.Use();
      }
    }

    private void ShowContextMenu()
    {
      List<FloatMenuOption> options = [];

      FloatMenuOption runSelectedOpt = new("Run Selected", delegate
      {
        List<UnitTestGroup> testGroups = selectedGroups.ToList();
        unitTestManager.Run(testGroups, selectedMethods);
      });
      if (!selectedGroups.Any() && !selectedMethods.Any())
      {
        runSelectedOpt.Disabled = false;
      }
      options.Add(runSelectedOpt);

      Find.WindowStack.Add(new FloatMenu(options));
    }

    private static void ProcessSelect<T>(HashSet<T> list, T item)
    {
      if (ShiftDown)
      {
        list.Add(item);
      }
      else if (ControlDown)
      {
        if (!list.Add(item))
        {
          list.Remove(item);
        }
      }
      else
      {
        list.Clear();
        list.Add(item);
      }
    }
  }
}