using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;
using Verse.Sound;

namespace DevTools.Testing;

internal sealed class Dialog_TestExplorer : Window
{
  private const float BtnSize = 30;
  private const float TestColumnWidth = 500;
  private const float MinPanelSize = 100;

  private static readonly Vector2 DialogSize = new(1280, 720);

  private static Rect dialogRect = new((UI.screenWidth - DialogSize.x) / 2f,
    (UI.screenHeight - DialogSize.y) / 2f, DialogSize.x, DialogSize.y);

  private readonly ITestManager testManager;
  private readonly IComparer<ITestGroup> comparer;
  private readonly List<ITestGroup> testGroups = [];

  private readonly StringBuilder summaryBuilder = new();

  private DataTable<ExplorerColumn, ITestGroup> table;
  private bool resizing;
  private float startingWidth;
  private float paneWidth = TestColumnWidth;

  public Dialog_TestExplorer(ITestManager testManager, IComparer<ITestGroup> comparer = null)
  {
    this.testManager = testManager;
    this.comparer = comparer ?? new TestExplorerEntryComparer();
    SetWindowProperties();
    Init();
  }

  private string Summary { get; set; }

  public override Vector2 InitialSize => dialogRect.size;

  public override bool IsDebug => true;

  protected override float Margin => 0;

  private void Init()
  {
    testGroups.Clear();
    IEnumerable<ITestGroup> groups = Test.GetGroups(testManager).OrderBy(group => group, comparer);
    testGroups.AddRange(groups);

    table = new DataTable<ExplorerColumn, ITestGroup>();
    table.SetColumns(
      new ExplorerColumn(ExplorerColumn.Type.Tests, TestColumnWidth),
      new ExplorerColumn(ExplorerColumn.Type.Duration, 150) { Anchor = TextAnchor.MiddleRight },
      //new ExplorerColumn(ExplorerColumn.Type.Traits, 150),
      new ExplorerColumn(ExplorerColumn.Type.ErrorMessage, 1000)
    );
    table.SetRows(testGroups);

    table.Selector = new TestSelector(testManager, table);
    table.Selector.OnSelectionChanged += RebuildSummary;
    table.RecacheHeight();
    table.SetComparer(comparer);
  }

  private void SetWindowProperties()
  {
    layer = WindowLayer.Super;
    closeOnAccept = false;
    preventDrawTutor = true;
    onlyDrawInDevMode = true;
    resizeable = true;
    draggable = true;
    doCloseX = true;
  }

  public override void PreOpen()
  {
    base.PreOpen();
    table.Selector?.Clear();
  }

  protected override void SetInitialSizeAndPosition()
  {
    windowRect = dialogRect.ClipInsideWindow().Rounded();
  }

  public override void DoWindowContents(Rect inRect)
  {
    using TextBlock resultText = new(GameFont.Small, TextAnchor.MiddleLeft);

    Rect rect = inRect.ContractedBy(base.Margin);

    Rect headerRect;
    using (new TextBlock(GameFont.Small, TextAnchor.UpperLeft))
    {
      headerRect = rect with { height = Text.LineHeight };
      Widgets.Label(headerRect, "Test Explorer");
    }

    headerRect.y += headerRect.height;
    DrawHeaderButtons(headerRect);
    rect.yMin = headerRect.yMax + 5;
    Rect tableRect = rect;
    if (table.Selector.AnySelected)
    {
      Rect infoPanelRect = tableRect with { xMin = tableRect.xMax - paneWidth, width = paneWidth };
      DrawInfoPanel(infoPanelRect);
      tableRect.xMax -= paneWidth;
      DoResizerButton(rect, tableRect with { yMax = inRect.yMax }, ref paneWidth);
    }
    table.DrawTable(tableRect);

    // Store as static so reopening window initializes
    // at the same position and size as before.
    dialogRect = windowRect;
  }

  private void DrawHeaderButtons(Rect rect)
  {
    const string RunName = "Run All";
    const string RunFailedName = "Run Failed Tests";
    const string RunNotRunName = "Run Not Run Tests";
    const string RunPassedName = "Run Passed Tests";
    const string RunTillFailName = "Run Until Failure";
    const string ClearAllName = "Clear All Test Results";

    Rect buttonRect = (rect with { size = new Vector2(BtnSize, BtnSize) }).ContractedBy(3);
    if (Widgets.ButtonImage(buttonRect, TexButton.SpeedButtonTextures[2], Color.green,
      tooltip: RunName))
    {
      SoundDefOf.Click.PlayOneShotOnCamera();
      testManager.RunAll();
    }
    buttonRect.x += buttonRect.width;
    if (Widgets.ButtonImage(buttonRect, TexButton.SpeedButtonTextures[1], Color.green,
      tooltip: RunName))
    {
      List<FloatMenuOption> options =
      [
        new(RunName, testManager.RunAll),
        new(RunFailedName,
          () => testManager
           .GetRunnerWith<StatusExpression>(Expression.Comparison.Equals, nameof(Status.Failed))
           .Run()),
        new(RunNotRunName,
          () => testManager
           .GetRunnerWith<StatusExpression>(Expression.Comparison.Equals, nameof(Status.NotRun))
           .Run()),
        new(RunPassedName,
          () => testManager
           .GetRunnerWith<StatusExpression>(Expression.Comparison.Equals, nameof(Status.Passed))
           .Run()),
        new(RunTillFailName,
          delegate
          {
            TestRunner runner = new(testManager);
            // Entire group can fail if exception was thrown
            runner.AddStopCondition(testCase => testCase.Status == Status.Failed);
            runner.Run();
          }),
        new(ClearAllName, () => Test.ResetAll(testManager))
      ];
      Find.WindowStack.Add(new FloatMenu(options));

      SoundDefOf.Click.PlayOneShotOnCamera();
    }
  }

  private void DrawInfoPanel(Rect inRect)
  {
    WidgetUtils.VerticalSeparator(inRect.x, inRect.y, inRect.height);

    Widgets.BeginGroup(inRect);
    inRect = inRect.AtZero().ContractedBy(5);
    inRect.xMin += 1;
    using (new TextBlock(GameFont.Medium, TextAnchor.MiddleLeft))
    {
      Rect labelRect = inRect with { height = ExplorerColumn.LineHeight };
      Widgets.Label(labelRect, "Summary");
      inRect.yMin = labelRect.yMax;
    }

    if (Summary != null)
    {
      Rect bottomRect = inRect with
      {
        x = inRect.width / 2f - 60,
        width = 120,
        yMin = inRect.yMax - 24
      };
      inRect.yMax -= 24;
      using TextBlock textBlock = new(TextAnchor.UpperLeft);
      Widgets.TextArea(inRect, Summary, readOnly: true);
      if (Widgets.ButtonText(bottomRect, "Open Log"))
      {
        testManager.OpenLogFile();
      }
    }
    Widgets.EndGroup();
  }

  private void DoResizerButton(Rect rect, Rect tableRect, ref float rightWindowSize)
  {
    const float ButtonSize = 24;

    float currentWidth = tableRect.width;

    Rect resizeButtonRect =
      new(tableRect.xMax - ButtonSize, tableRect.yMax - ButtonSize, ButtonSize, ButtonSize);
    Vector2 mousePosition = Event.current.mousePosition;
    if (Input.GetMouseButtonDown(0) && Mouse.IsOver(resizeButtonRect))
    {
      resizing = true;
      startingWidth = mousePosition.x - 16;
    }
    if (resizing)
    {
      tableRect.width = startingWidth + (mousePosition.x - startingWidth);
      tableRect.width = Mathf.Clamp(tableRect.width, TestColumnWidth, rect.width - MinPanelSize);

      if (!Input.GetMouseButton(0))
      {
        resizing = false;
      }
    }
    Widgets.ButtonImage(resizeButtonRect, TexUI.WinExpandWidget);

    if (!Mathf.Approximately(tableRect.width, currentWidth))
    {
      rightWindowSize = rect.width - tableRect.width;
    }
  }

  private void RebuildSummary()
  {
    if (!table.Selector.AnySelected)
    {
      Summary = null;
      return;
    }
    try
    {
      Assert.IsTrue(summaryBuilder.Length == 0);
      ITestGroup singleSelected = table.Selector.selected.First();
      if (table.Selector.selected.Count == 1)
      {
        summaryBuilder.AppendLine(singleSelected.Label);
      }
      summaryBuilder.AppendLine($"    Tests in group: {table.Selector.selected.Sum(static group => group.TestCount)}");
      summaryBuilder.AppendLine();
      summaryBuilder.AppendLine("Outcomes");
      AppendOutcomeSummary(table.Selector.selected, summaryBuilder);

      if (table.Selector.selected.Count == 1 &&
          singleSelected.Status is Status.Failed or Status.Canceled or Status.Skipped)
      {
        if (!singleSelected.FailMessage.NullOrEmpty())
        {
          summaryBuilder.AppendLine(singleSelected.FailMessage);
        }
        if (singleSelected.StackTrace != null)
        {
          summaryBuilder.AppendLine();
          summaryBuilder.AppendLine(singleSelected.StackTrace.ToString());
        }
      }
      Summary = summaryBuilder.ToString();
    }
    finally
    {
      summaryBuilder.Clear();
    }
    return;

    static void AppendOutcomeSummary(IEnumerable<ITestGroup> groups, StringBuilder summaryBuilder)
    {
      TestReport report = new();
      foreach (ITestGroup group in groups)
      {
        report.Add(group);
      }

      AppendStatus(Status.NotRun);
      AppendStatus(Status.Skipped);
      AppendStatus(Status.Passed);
      AppendStatus(Status.Failed);
      return;

      void AppendStatus(Status status)
      {
        int count = report.Count(status);
        if (count > 1 || (status is not Status.Passed && count > 0))
        {
          summaryBuilder.AppendLine($"    {count} {status}");
        }
      }
    }
  }

  public class TestExplorerEntryComparer : Comparer<ITestGroup>
  {
    public override int Compare(ITestGroup a, ITestGroup b)
    {
      if (a == null && b == null)
        return 0;
      if (b == null)
        return 1;
      if (a == null)
        return -1;
      return StringComparer.InvariantCultureIgnoreCase.Compare(a.Label, b.Label);
    }
  }
}