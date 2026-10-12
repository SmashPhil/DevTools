using System.Collections.Generic;
using Verse;

namespace DevTools.Testing;

internal class TestPlan
{
	public string name = null;
	public List<TestPlanJob> jobs = null;

	public bool IsValid => !jobs.NullOrEmpty();

  public List<ITestFixture> Fixtures { get; } = [];
}