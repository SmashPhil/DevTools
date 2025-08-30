using System.Collections.Generic;
using Verse;

namespace DevTools.UnitTesting;

internal class TestPlan
{
	public string name;

	//public bool continueOnFailure = true;
	public List<Batch> steps;

	public bool TryDoPostLoad()
	{
		if (steps.NullOrEmpty())
		{
			Log.Error($"{name} test plan is empty.");
			return false;
		}
		return true;
	}

	public class Batch
	{
		public List<string> loadWithMods;
	}
}