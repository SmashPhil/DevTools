using System;
using DevTools.Benchmarking;
using JetBrains.Annotations;

namespace DevTools.Testing;

/// <summary>
/// Test entry in explorer window. Contains info related to the test(s) being executed
/// </summary>
[PublicAPI]
public interface ITestCase : IDataRow<ExplorerColumn>
{
	/// <summary>
	/// Test name shown in test explorer
	/// </summary>
	string Name { get; }

	/// <summary>
	/// Class type containing this test case
	/// </summary>
	Type Type { get; }

	/// <summary>
	/// Total test count contained
	/// </summary>
	int TestCount { get; }

	/// <summary>
	/// Time to execute this test case
	/// </summary>
	Benchmark.Result Duration { get; }

	/// <summary>
	/// Final test status. Highest severity Status is preserved
	/// </summary>
	Status Status { get; set; }

	/// <summary>
	/// Shortened label for test failure shown in test explorer
	/// </summary>
	string FailLabel { get; }

	/// <summary>
	/// Reason for test failure
	/// </summary>
	string FailMessage { get; }

	/// <summary>
	/// Meta data container for key/value lookups
	/// </summary>
	MetaDataContainer MetaData { get; }

	/// <summary>
	/// Reset results and Status
	/// </summary>
	/// <remarks>Reset call should be propagated to any contained functions or groups.</remarks>
	void Reset();

	/// <summary>
	/// Manually fail test case
	/// </summary>
	/// <param name="reason">Failure reason</param>
	void Fail(string reason);

	/// <summary>
	/// Summary count of all test results
	/// </summary>
	void TestOutcomes(StatusCount statusCount);
}