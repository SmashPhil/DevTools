using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LudeonTK;
using Verse;

namespace DevTools.QuickTesting;

/// <summary>
/// Simple A-B test for detecting startup errors and doing a quick runthrough of all defs added by this modlist
/// to verify that there are no immediate startup or runtime errors.
/// </summary>
internal class QuickTestManager : IDevTool
{
	private const string ManagerName = "QuickTest";

	string IDevTool.ToolName => ManagerName;

	void IDevTool.Init(ModContentPack mod)
	{
	}

	void IDevTool.OpenMenu()
	{
	}

	bool IDevTool.TryRegisterType(Type type)
	{
		throw new NotImplementedException();
	}
}