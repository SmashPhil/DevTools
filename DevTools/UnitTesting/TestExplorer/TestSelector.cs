using System.Collections.Generic;
using Verse;

namespace DevTools.UnitTesting;

internal class TestSelector : SelectionManager<ITestCase>
{
  private readonly UnitTestManager unitTestManager;

  public TestSelector(UnitTestManager unitTestManager)
  {
    this.unitTestManager = unitTestManager;
  }

  protected override void ShowContextMenu()
  {
    List<FloatMenuOption> options = [];
    // ReSharper disable UseObjectOrCollectionInitializer
    FloatMenuOption runSelectedOpt =
      new("Run Selected", unitTestManager.GetRunnerWith(SelectedFilter).Run);
    runSelectedOpt.Disabled = !AnySelected;
    options.Add(runSelectedOpt);

    Find.WindowStack.Add(new FloatMenu(options));
  }

  private IEnumerable<(ITestGroup, List<ITestFunction>)> SelectedFilter(
    UnitTestManager unitTestManager)
  {
    foreach (UnitTestGroup group in unitTestManager.UnitTests)
    {
      List<ITestFunction> functions = [];
      bool runAllInGroup = selected.Contains(group);
      foreach (ITestFunction testFunction in group.TestFunctions)
      {
        if (runAllInGroup || selected.Contains(testFunction))
          functions.Add(testFunction);
      }
      if (!functions.NullOrEmpty())
        yield return (group, functions);
    }
  }
}