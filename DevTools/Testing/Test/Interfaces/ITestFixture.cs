using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace DevTools.Testing;

/// <summary>
/// Container of tests to be executed. Will execute SetUp and TearDown
/// before and after any of its tests run.
/// </summary>
[PublicAPI]
public interface ITestFixture : ITestCase
{
  /// <summary>
  /// Class type to instantiate for this fixture
  /// </summary>
  Type Type { get; }

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
  /// Constructs an instance of the type for running this fixture's test functions on.
  /// </summary>
  object CreateInstance();

  /// <summary>
  /// Called once before any test function in this group is executed.
  /// </summary>
  bool OneTimeSetUp(object instance);

  /// <summary>
  /// Called once after all test functions in this group have finished executing.
  /// </summary>
  bool OneTimeTearDown(object instance);

  /// <summary>
  /// Called right before any test function is executed
  /// </summary>
  bool SetUp(object instance);

  /// <summary>
  /// Called immediately after a test function is executed.
  /// </summary>
  bool TearDown(object instance);
}