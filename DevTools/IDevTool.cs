using System;
using Verse;

namespace DevTools;

internal interface IDevTool
{
	void Init(ModContentPack mod);

	bool TryRegisterType(Type type);
}