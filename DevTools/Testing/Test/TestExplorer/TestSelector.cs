using System.Collections.Generic;
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
    // ReSharper disable UseObjectOrCollectionInitializer
    FloatMenuOption runSelectedOpt =
      new("Run Selected", testManager.GetRunnerWith(SelectedFilter).Run);
    runSelectedOpt.Disabled = !AnySelected;
    options.Add(runSelectedOpt);
    Find.WindowStack.Add(new FloatMenu(options));
  }

  private IEnumerable<(ITestFixture, List<ITestFunction>)> SelectedFilter(ITestManager manager)
  {
    HashSet<ITestFixture> selectedFixtures = [];
    HashSet<ITestFunction> selectedFunctions = [];

    foreach (ITestGroup group in selected)
    {
      AddGroup(group);
    }

    foreach (ITestFixture fixture in manager.TestFixtures)
    {
      bool runAllInFixture = selectedFixtures.Contains(fixture);
      List<ITestFunction> functions = [];

      foreach (ITestFunction function in fixture.TestFunctions)
      {
        Assert.IsTrue(function.MethodType is MethodType.Test);
        if (runAllInFixture || selectedFunctions.Contains(function))
        {
          functions.Add(function);
        }
      }

      if (functions.Count > 0)
      {
        yield return (fixture, functions);
      }
    }
    yield break;

    void AddGroup(ITestGroup group)
    {
      switch (group.TestCase)
      {
        case ITestFixture fixture:
          selectedFixtures.Add(fixture);
          break;
        case ITestFunction function:
          Assert.IsTrue(function.MethodType is MethodType.Test);
          selectedFunctions.Add(function);
          break;
        default:
          foreach (ITestGroup child in group.Children)
          {
            AddGroup(child);
          }
          break;
      }
    }
  }
}