using System.Collections;
using System.Collections.Generic;

namespace DevTools.Testing;

internal class TestCaseComparer : IComparer<ITestCase>
{
	public static readonly TestCaseComparer Default = new();

	int IComparer<ITestCase>.Compare(ITestCase lhs, ITestCase rhs)
	{
		// There should never be any null Method entries. TestFixtureManager was not initialized
		// properly and testing may throw as well.
		if (lhs is null)
			return 1;
		if (rhs is null)
			return -1;

		int lhsInt = lhs.MetaData.Get<int>(MetaDataName.ExecutionPriority);
		int rhsInt = rhs.MetaData.Get<int>(MetaDataName.ExecutionPriority);

		// Higher priority => earlier in the list
		if (lhsInt == rhsInt)
			return 0;
		if (lhsInt > rhsInt)
			return -1;
		return 1;
	}
}