using System.Collections.Generic;
using JetBrains.Annotations;
using Verse;

namespace DevTools.Testing;

internal class TestPlan
{
	public string name;
	public List<TestPlanJob> jobs;

	public bool IsValid => !jobs.NullOrEmpty();

  public List<ITestFixture> Fixtures { get; } = [];
}