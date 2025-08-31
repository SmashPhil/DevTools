using System.Collections.Generic;
using Verse;

namespace DevTools.Testing;

internal class TestSelector : SelectionManager<ITestCase>
{
	private readonly ITestManager testManager;

	public TestSelector(ITestManager testManager)
	{
		this.testManager = testManager;
	}

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

	private IEnumerable<(ITestGroup, List<ITestFunction>)> SelectedFilter(ITestManager testManager)
	{
		foreach (ITestGroup group in testManager.TestGroups)
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