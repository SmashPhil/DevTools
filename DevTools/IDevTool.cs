using System;
using Verse;

namespace DevTools;

internal interface IDevTool
{
	bool Init(ModContentPack mod);

	bool TryRegisterType(Type type);
}