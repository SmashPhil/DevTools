using System.Collections.Generic;
using JetBrains.Annotations;
using Verse;

namespace DevTools.Testing;

[PublicAPI]
internal class TestPlan
{
	public string name;
	public List<TestPlanJob> jobs;
	public TestPlanConfig config = new();

	public bool IsValid => !jobs.NullOrEmpty() && config != null;

	public void PostLoadInit(ModContentPack mod)
	{
		foreach (TestPlanJob job in jobs)
		{
			job.PostLoadInit(mod);
		}
	}
}