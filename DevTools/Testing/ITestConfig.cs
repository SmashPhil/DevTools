using JetBrains.Annotations;
using UnityEngine;

namespace DevTools.Testing;

/// <summary>
/// Provides configuration consumed by the test manager and runner for execution behavior and testbed setup.
/// </summary>
[PublicAPI]
public interface ITestConfig
{
	/// <returns>
	/// <see langword="true"/> if the runner should stop after the first failure. <see langword="false"/> if the runner
	/// should continue to the end regardless of test results.
	/// </returns>
	bool StopOnFailure { get; }

	/// <summary>
	/// Number of retry attempts the runner can make after a test case fails.
	/// </summary>
	int RetryAttempts { get; }

	/// <summary>
	/// <see cref="Logger"/> configurations for settings related to <see cref="DevLog"/>
	/// </summary>
	Logger.Config LogConfig { get; }

	/// <summary>
	/// Root seed assigned to <see cref="Verse.Rand"/> for the duration of the runner's lifetime.
	/// </summary>
	/// <remarks>
	/// Use for predetermined pseudo-randomness where values returned from <see cref="Verse.Rand"/> should be deterministic.
	/// </remarks>
	uint? Seed { get; }

	/// <summary>
	/// Configurations related to world generation when transitioning to <see cref="TestType.Playing"/>
	/// </summary>
	WorldGenerationSettings WorldSettings { get; }

	/// <summary>
	/// Configurations related to map generation when initial test map is generated.
	/// </summary>
	MapGenerationSettings MapSettings { get; }

	/// <returns>
	/// <see langword="true"/> if test cases should fail when any warning is logged. <see langword="false"/> if warnings should be ignored.
	/// </returns>
	bool FailOnWarnings { get; }

	/// <returns>
	/// <see langword="true"/> if test cases should fail when any error is logged. <see langword="false"/> if errors should be ignored.
	/// </returns>
	bool FailOnErrors { get; }

	/// <summary>
	/// Determines if a specific log that will trigger a test case failure should be ignored.
	/// </summary>
	/// <remarks>
	/// Typical use is regex based suppression for errors or warnings coming from other sources that should not case the
	/// test run to fail.
	/// </remarks>
	/// <returns>
	/// <see langword="true"/> if test case should fail. <see langword="false"/> if warnings should be ignored.
	/// </returns>
	bool SupressLogFailure(LogType logType, string message);

	/// <summary>
	/// Runs after the config file is loaded from disk.
	/// </summary>
	void PostLoad();
}