using System.Collections.Generic;
using System.Linq;
using Verse;

namespace DevTools.Testing;

public class TestPlan
{
	public string name = null;
	public List<TestPlanJob> jobs = null;

	public bool IsValid => !jobs.NullOrEmpty();

  public List<ITestFixture> Fixtures { get; } = [];

  public IEnumerable<(ITestFixture, List<ITestFunction>)> Filter(ITestManager _)
  {
    foreach (ITestFixture fixture in Fixtures)
    {
      yield return (fixture, fixture.TestFunctions.ToList());
    }
  }
}