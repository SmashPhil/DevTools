using System;
using System.Collections.Generic;
using System.Diagnostics;
using DevTools.Benchmarking;
using JetBrains.Annotations;

namespace DevTools.Testing;

[PublicAPI]
public interface ITestGroup : IDataRow<ExplorerColumn>
{
  /// <summary>
  /// Top level test case for this group
  /// </summary>
  ITestCase TestCase { get; }

  /// <summary>
  /// Parent of this test group
  /// </summary>
  ITestGroup Parent { get; }

  /// <summary>
  /// Gets the collection of child test groups contained within this group.
  /// </summary>
  IEnumerable<ITestGroup> Children { get; }

  /// <summary>
  /// Final test status. Highest severity Status is preserved
  /// </summary>
  Status Status { get; set; }

  /// <summary>
  /// Total test count contained
  /// </summary>
  int TestCount { get; }
    
  /// <summary>
  /// Name of the Expect or Assert condition being evaluated
  /// </summary>
  string TestContext { get; set; }

  /// <summary>
  /// Shortened label for test failure shown in test explorer
  /// </summary>
  string FailLabel { get; set; }

  /// <summary>
  /// Reason for test failure
  /// </summary>
  string FailMessage { get; set; }

  /// <summary>
  /// Stacktrace for failed test case
  /// </summary>
  StackTrace StackTrace { get; set; }

  /// <summary>
  /// Exception thrown from test
  /// </summary>
  Exception Exception { get; set; }

  /// <summary>
  /// Time to execute this test case
  /// </summary>
  Benchmark.Result Duration { get; set; }

  /// <summary>
  /// Reset results and Status
  /// </summary>
  /// <remarks>Reset call should be propagated to any contained functions or groups.</remarks>
  void Reset();
}
