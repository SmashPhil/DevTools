namespace DevTools;

internal interface IDevToolWithMenu : IDevTool
{
	string Name { get; }

	void OpenMenu();
}