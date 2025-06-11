using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;
using Verse.Sound;

namespace DevTools.UnitTesting;

internal sealed class Dialog_TestExplorer : Window
{
  private const float BtnSize = 30;
  private const float TestColumnWidth = 350;
  private const float MinPanelSize = 100;

  private static readonly Vector2 dialogSize = new(1280, 720);

  private static Rect dialogRect = new((UI.screenWidth - dialogSize.x) / 2f,
    (UI.screenHeight - dialogSize.y) / 2f, dialogSize.x, dialogSize.y);

  private readonly UnitTestManager unitTestManager;
  private readonly List<UnitTestGroup> testGroups = [];
  private readonly DataTable<ExplorerColumn, ITestCase> table = new();

  private readonly StringBuilder summaryBuilder = new();

  private bool resizing;
  private float startingWidth;
  private float paneWidth = TestColumnWidth;

  public Dialog_TestExplorer(UnitTestManager unitTestManager)
  {
    this.unitTestManager = unitTestManager;
    SetWindowProperties();
  }

  private string Summary { get; set; }

  public override Vector2 InitialSize => dialogRect.size;

  public override bool IsDebug => true;

  protected override float Margin => 0;

  private void RefreshTestList()
  {
    testGroups.Clear();
    testGroups.AddRange(unitTestManager.UnitTests.OrderBy(group => group.Name));
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
    RefreshTestList();
    table.SetColumns(
      new ExplorerColumn(ExplorerColumn.Type.Tests, TestColumnWidth),
      new ExplorerColumn(ExplorerColumn.Type.Duration, 150) { Anchor = TextAnchor.MiddleRight },
      //new ExplorerColumn(ExplorerColumn.Type.Traits, 150),
      new ExplorerColumn(ExplorerColumn.Type.ErrorMessage, 1000)
    );
    table.SetRows(testGroups);
    table.SetSelector(new TestSelector(unitTestManager));
    table.Selector.OnSelectionChanged += RebuildSummary;
    table.RecacheHeight();
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
      unitTestManager.RunAll();
    }
    buttonRect.x += buttonRect.width;
    if (Widgets.ButtonImage(buttonRect, TexButton.SpeedButtonTextures[1], Color.green,
      tooltip: RunName))
    {
      List<FloatMenuOption> options =
      [
        new(RunName, unitTestManager.RunAll),
        new(RunFailedName,
          () => unitTestManager
           .GetRunnerWith<StatusExpression>(Expression.Comparison.Equals, nameof(Status.Failed))
           .Run()),
        new(RunNotRunName,
          () => unitTestManager
           .GetRunnerWith<StatusExpression>(Expression.Comparison.Equals, nameof(Status.NotRun))
           .Run()),
        new(RunPassedName,
          () => unitTestManager
           .GetRunnerWith<StatusExpression>(Expression.Comparison.Equals, nameof(Status.Passed))
           .Run()),
        new(RunTillFailName,
          delegate
          {
            TestRunner runner = new(unitTestManager);
            // Entire group can fail if exception was thrown
            runner.AddStopCondition((ITestGroup group) => group.Status == Status.Failed);
            runner.AddStopCondition((ITestFunction function) => function.Status == Status.Failed);
            runner.Run();
          }),
        new(ClearAllName, unitTestManager.ClearTestResults),
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
        x = inRect.width / 2f - 60, width = 120, yMin = inRect.yMax - 24
      };
      inRect.yMax -= 24;
      using TextBlock textBlock = new(TextAnchor.UpperLeft);
      Widgets.TextArea(inRect, Summary, readOnly: true);
      if (Widgets.ButtonText(bottomRect, "Open Log"))
      {
        unitTestManager.OpenLogFile();
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

      ITestCase singleSelected = table.Selector.selected.FirstOrDefault();
      Assert.IsNotNull(singleSelected);
      if (table.Selector.selected.Count == 1)
        summaryBuilder.AppendLine(singleSelected.Name);
      StatusCount count = new();
      foreach (ITestCase testCase in table.Selector.selected)
      {
        testCase.TestOutcomes(count);
      }
      summaryBuilder.AppendLine($"    Tests in group: {count.Total}");
      summaryBuilder.AppendLine();
      summaryBuilder.AppendLine("Outcomes");
      AppendOutcomeSummary(count, summaryBuilder);

      if (table.Selector.selected.Count == 1)
        summaryBuilder.AppendLine(singleSelected.FailMessage);

      Summary = summaryBuilder.ToString();
    }
    finally
    {
      summaryBuilder.Clear();
    }
  }

  private static void AppendOutcomeSummary(StatusCount statusCount, StringBuilder summaryBuilder)
  {
    summaryBuilder.AppendLine(
      $"    {statusCount[MethodType.Test, Status.Passed]} Passed");
    summaryBuilder.AppendLine(
      $"    {statusCount[MethodType.Test, Status.Failed]} Failed");
    if (statusCount.AssertFailCount > 0)
      summaryBuilder.AppendLine(
        $"    {statusCount.AssertFailCount} Assert Failed");
    if (statusCount.ExceptionCount > 0)
      summaryBuilder.AppendLine(
        $"    {statusCount.ExceptionCount} Exception Thrown");

    int prepareFailed = statusCount[MethodType.SetUp, Status.Failed];
    if (prepareFailed > 0)
      summaryBuilder.AppendLine($"    {prepareFailed} SetUp Failed");
    int prepareCanceled = statusCount[MethodType.SetUp, Status.Canceled];
    if (prepareCanceled > 0)
      summaryBuilder.AppendLine($"    {prepareCanceled} SetUp Canceled");
    int prepareSkipped = statusCount[MethodType.SetUp, Status.Skipped];
    if (prepareSkipped > 0)
      summaryBuilder.AppendLine($"    {prepareSkipped} SetUp Skipped");

    int cleanUpFailed = statusCount[MethodType.TearDown, Status.Failed];
    if (cleanUpFailed > 0)
      summaryBuilder.AppendLine($"    {cleanUpFailed} TearDown Failed");
    int cleanUpCanceled = statusCount[MethodType.TearDown, Status.Canceled];
    if (cleanUpCanceled > 0)
      summaryBuilder.AppendLine($"    {cleanUpCanceled} TearDown Canceled");
    int cleanUpSkipped = statusCount[MethodType.TearDown, Status.Skipped];
    if (cleanUpSkipped > 0)
      summaryBuilder.AppendLine($"    {cleanUpSkipped} TearDown Skipped");
  }
}