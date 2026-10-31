using System.Collections.Generic;
using Verse;

namespace DevTools.Testing;

public class TestPlan
{
	public string name = null;

	public List<TestPlanJob> jobs = null;

	public bool IsValid => !jobs.NullOrEmpty();

  public HashSet<ITestFixture> Fixtures { get; } = [];

  internal string FilePath { get; set; }

  public bool Filter(ITestFixture fixture, ITestFunction _)
  {
    return Fixtures.Contains(fixture);
  }
}