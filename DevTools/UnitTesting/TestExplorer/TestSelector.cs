using System.Collections.Generic;
using System.Linq;
using Verse;

namespace DevTools.UnitTesting;

internal class TestSelector : SelectionManager
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
    FloatMenuOption runSelectedOpt = new("Run Selected",
      delegate
      {
        unitTestManager.Run(
          [.. selected.Where(item => item is UnitTestGroup).Cast<UnitTestGroup>()],
          [.. selected.Where(item => item is UnitTestGroup.Method).Cast<UnitTestGroup.Method>()]);
      });
    runSelectedOpt.Disabled = !AnySelected;
    options.Add(runSelectedOpt);

    Find.WindowStack.Add(new FloatMenu(options));
  }
}