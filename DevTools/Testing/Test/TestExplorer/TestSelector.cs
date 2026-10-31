using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;
using Verse;

namespace DevTools.Testing;

internal class TestSelector : SelectionManager<ITestGroup>
{
  private readonly ITestManager testManager;
  private readonly DataTable<ExplorerColumn, ITestGroup> dataTable;

  public TestSelector(ITestManager testManager, DataTable<ExplorerColumn, ITestGroup> dataTable)
  {
    this.testManager = testManager;
    this.dataTable = dataTable;
  }

  protected override IEnumerable<ITestGroup> AllItems => dataTable.AllRows;

  protected override void ShowContextMenu()
  {
    List<FloatMenuOption> options = [];
    TestRunner runner = new(testManager);
    runner.AddFilter(SelectorFilter);
    FloatMenuOption runSelectedOpt = new("Run Selected", runner.Run)
    {
      Disabled = !AnySelected
    };
    options.Add(runSelectedOpt);
    Find.WindowStack.Add(new FloatMenu(options));
  }

  private bool SelectorFilter(ITestFixture fixture, ITestFunction function)
  {
    return IsSelected(fixture.Module) ||
           IsSelected(fixture) ||
           IsSelected(function);
  }

  private bool IsSelected(ITestCase testCase)
  {
    Assert.IsNotNull(testCase);
    return selected.Contains(Test.GetEntry(testCase));
  }

  private bool IsSelected(ITestModule module)
  {
    Assert.IsNotNull(module);
    return selected.Contains(Test.GetEntry(module));
  }
}