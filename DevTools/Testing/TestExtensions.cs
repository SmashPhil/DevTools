using System.Collections;
using Verse;

namespace DevTools.Testing;

internal static class TestExtensions
{
	public static bool IsSubRoutine(this ITestFunction testFunction)
	{
		return testFunction.MethodInfo.ReturnType == typeof(IEnumerator);
	}

	public static bool IsDisabled(this ITestCase testCase)
	{
		if (testCase.MetaData.Get<string[]>(MetaDataName.LoadIfAllModsActive) is { } packageIdsAll &&
			!ModLister.AllModsActiveNoSuffix(packageIdsAll))
		{
			return false;
		}
		if (testCase.MetaData.Get<string[]>(MetaDataName.LoadIfAnyModsActive) is { } packageIdsAny &&
			!ModLister.AnyModActiveNoSuffix(packageIdsAny))
		{
			return false;
		}
		return testCase.MetaData.Get<bool>(MetaDataName.Disabled);
	}
}