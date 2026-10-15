using System.Collections.Generic;
using Verse;

namespace DevTools.Testing;

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