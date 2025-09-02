using System;
using System.Runtime.CompilerServices;
using Verse;

namespace DevTools;

internal static class BatchModeCompatibility
{
	public static void Enable()
	{
		Log.Message("Enabling batch mode compatibility.");
		TriggerProblematicTypes();
	}

	private static void TriggerProblematicTypes()
	{
		try
		{
			RuntimeHelpers.RunClassConstructor(typeof(Text).TypeHandle);
		}
		catch (TypeInitializationException)
		{
			// Absorb type init errors from running headless as Ludeon calls to various GUI related types with the assumption
			// that these types will be pre-initialized from GUI passes which do not run in headless, triggering GUI method
			// calls from non-GUI methods.
		}
	}
}