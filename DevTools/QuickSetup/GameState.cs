using System;

namespace DevTools.GameAction;

[Flags]
public enum GameState
{
	OnStartup = 1 << 0,
	NewGame = 1 << 1,
	LoadedSave = 1 << 2,

	Playing = NewGame | LoadedSave
}