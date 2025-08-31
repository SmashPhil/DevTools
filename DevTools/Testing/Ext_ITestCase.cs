namespace DevTools.Testing;

internal static class Ext_ITestCase
{
	public static string NameWithCount(this ITestCase testCase)
	{
		return testCase is ITestGroup { TestCount: > 1 } ?
			$"{testCase.Name} ({testCase.TestCount})" :
			testCase.Name;
	}
}