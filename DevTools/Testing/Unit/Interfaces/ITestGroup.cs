using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace DevTools.Testing;

/// <summary>
/// Container of tests to be executed. Will execute SetUp and TearDown
/// before and after any of its tests run.
/// </summary>
[PublicAPI]
public interface ITestGroup : ITestCase, IComparable<ITestGroup>
{
	/// <summary>
	/// Scene setup for executing tests in this group
	/// </summary>
	TestType TestType { get; }

	/// <summary>
	/// Save file to load before execution of any tests within this group.
	/// </summary>
	string SaveFile { get; }

	/// <summary>
	/// All test functions in this group
	/// </summary>
	IEnumerable<ITestFunction> TestFunctions { get; }

	/// <summary>
	/// SetUp functions before any test methods can execute
	/// </summary>
	/// <remarks>All SetUp methods were executed successfully.</remarks>
	bool SetUp();

	/// <summary>
	/// TearDown functions that run after all test methods are executed
	/// </summary>
	/// <remarks>All TearDown methods were executed successfully.</remarks>
	bool TearDown();
}