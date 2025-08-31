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
			!ModsConfig.AreAllActive(packageIdsAll))
		{
			return false;
		}
		if (testCase.MetaData.Get<string[]>(MetaDataName.LoadIfAnyModsActive) is { } packageIdsAny &&
			!ModsConfig.IsAnyActiveOrEmpty(packageIdsAny, trimNames: true))
		{
			return false;
		}
		return testCase.MetaData.Get<bool>(MetaDataName.Disabled);
	}
}