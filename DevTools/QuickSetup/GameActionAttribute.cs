using System;

namespace DevTools.GameAction;

/// <summary>
/// Invoke a static method at a specific <see cref="DevTools.GameAction.GameState"/> to set up for playground testing
/// or troubleshooting.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class GameActionAttribute : Attribute
{
	/// <summary>
	/// Name of option in GameAction menu.
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// GameState to invoke action
	/// </summary>
	/// <remarks>
	/// <para><see cref="GameState.OnStartup"/> executes after the main menu has loaded</para>
	/// <para><see cref="GameState.NewGame"/> executes after a new game has been initialized</para>
	/// <para><see cref="GameState.LoadedSave"/> executes after a loaded save has finished initializing</para>
	/// <para><see cref="GameState.Playing"/> executes for both new games and loaded saves</para>
	/// </remarks>
	public GameState GameState { get; set; } = GameState.Playing;
}