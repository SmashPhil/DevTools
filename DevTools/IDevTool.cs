using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevTools;

internal interface IDevTool
{
  string ToolName { get; }

  bool TryRegisterType(Type type);

  void Init();

  void OpenMenu();
}