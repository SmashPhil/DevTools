using System.Collections.Generic;
using Verse;

namespace DevTools.Testing;

// Enum values are ordered from worst to best in the context that lower
// values will have priority on tabulated test results. Order matters!!
public enum Status
{
	Failed,
	Canceled,
	Skipped,
	Passed,
	Pending,
	NotRun,
}

internal static class StatusComparer
{
	public static Status Min(this Status current, params Status[] others)
	{
		if (others.NullOrEmpty())
			return current;

		foreach (Status next in others)
		{
			current = Comparer<Status>.Default.Compare(current, next) <= 0 ? current : next;
		}
		return current;
	}
}