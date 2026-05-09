using JetBrains.Annotations;

namespace DevTools.Testing;

/// <summary>
/// Defines lifecycle hooks invoked by the test runner.
/// </summary>
[PublicAPI]
public interface ITestActions
{
	/// <summary>
	/// Called immediately before the test runner executes a test group.
	/// </summary>
	/// <param name="group">The test group that is about to be executed.</param>
	/// <returns>
	/// <see langword="true"/> to continue with execution of <paramref name="group"/>;
	/// <see langword="false"/> to signal a pre-test failure.
	/// </returns>
	/// <remarks>
	/// Exceptions thrown here are treated as failures by the runner.
	/// </remarks>
	bool PreTest(ITestFixture group);

	/// <summary>
	/// Called immediately after the test runner finishes executing a test group's tear down functions, regardless of outcome.
	/// Use this hook for cleanup, diagnostics collection, or reporting.
	/// </summary>
	/// <param name="group">The test group that has just executed.</param>
	/// <returns>
	/// <see langword="true"/> to indicate post-processing completed successfully;
	/// <see langword="false"/> to indicate a post-test failure that should be surfaced by the runner.
	/// </returns>
	/// <remarks>
	/// Exceptions thrown here are treated as failures by the runner.
	/// </remarks>
	bool PostTest(ITestFixture group);

	/// <summary>
	/// Called after a test case completes (either a group or an individual function) to decide whether execution should stop.
	/// </summary>
	/// <param name="testCase">The test case that has just completed.</param>
	/// <returns>
	/// <see langword="true"/> to request the runner to halt further execution.
	/// <see langword="false"/> to continue with the next scheduled test case.
	/// </returns>
	/// <remarks>
	/// Typical uses include "stop on first failure" or early-exit policies based on outcomes or runtime metrics.
	/// </remarks>
	bool ShouldStop(ITestCase testCase);
}