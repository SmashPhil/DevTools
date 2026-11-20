using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace DevTools;

public readonly struct StackTraceCacheDisabler : IDisposable
{
	private static readonly FieldInfo NoStacktraceCaching;

	private readonly bool? oldValue;

	static StackTraceCacheDisabler()
	{
		// Another mod can supply Harmony without the standalone mod's settings.
		Type harmonyMain = GenTypes.GetTypeInAnyAssembly("HarmonyMod.Settings");
		if (harmonyMain != null)
			NoStacktraceCaching = AccessTools.Field(harmonyMain, "noStacktraceCaching");
	}

	public StackTraceCacheDisabler()
	{
		if (NoStacktraceCaching == null)
			return;

		oldValue = (bool)NoStacktraceCaching.GetValue(null);
		NoStacktraceCaching.SetValue(null, true);
	}

	public void Dispose()
	{
		if (oldValue is bool previousValue)
			NoStacktraceCaching.SetValue(null, previousValue);
	}
}