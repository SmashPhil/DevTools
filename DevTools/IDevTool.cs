using System;
using Verse;

namespace DevTools;

internal interface IDevTool
{
  string ToolName { get; }

  bool TryRegisterType(Type type);

  void Init(ModContentPack mod);

  void OpenMenu();
}