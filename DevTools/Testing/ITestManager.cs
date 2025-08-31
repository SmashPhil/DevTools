using System.Collections.Generic;
using JetBrains.Annotations;

namespace DevTools.Testing;

/// <summary>
/// Manager contract used by the test runner for configuration and test discovery.
/// </summary>
[PublicAPI]
public interface ITestManager
{
	/// <summary>
	/// Name of the config file from the Configs/ directory in the active mod's root folder.
	/// </summary>
	string ConfigName { get; }

	/// <summary>
	/// Config file associated with this manager.
	/// </summary>
	ITestConfig Config { get; }

	/// <summary>
	/// All test groups (fixtures) eligible for execution from the test runner.
	/// </summary>
	IEnumerable<ITestGroup> TestGroups { get; }

	/// <summary>
	/// Queue up all test groups for testing from a test runner.
	/// </summary>
	void RunAll();

	/// <summary>
	/// TestRunner callback when the test run has finished.
	/// </summary>
	void OnTestRunnerEnd();
}