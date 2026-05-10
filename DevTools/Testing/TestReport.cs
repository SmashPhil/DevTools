using System.Linq;
using JetBrains.Annotations;

namespace DevTools.Testing;

internal class TestReport
{
  private int passed;
  private int failed;
  private int skipped;
  private int notRun;

  /// <summary>
  /// Count of status aggregated at this point.
  /// </summary>
  /// <param name="status">Status to retrieve count for.</param>
  /// <returns>
  /// Number of tests with final Status equal to <paramref name="status"/>
  /// </returns>
  public int Count(Status status)
  {
    return status switch
    {
      Status.Failed => failed,
      Status.Skipped => skipped,
      Status.Passed => passed,
      Status.NotRun => notRun,
      _ => 0
    };
  }

  /// <summary>
  /// Adds the results of the test group and its child groups to the current aggregate counts.
  /// </summary>
  /// <remarks>
  /// This method updates the internal counters for passed, failed, skipped, and not run tests based on
  /// the status of the provided test group and all of its descendants.
  /// </remarks>
  /// <param name="testGroup">
  /// The test group whose results are to be included. If the group contains child groups, their results
  /// are also aggregated recursively.
  /// </param>
  public void Add([NotNull] in ITestGroup testGroup)
  {
    if (testGroup.Children.Any())
    {
      foreach (ITestGroup child in testGroup.Children)
      {
        Add(child);
      }
    }
    else
    {
      switch (testGroup.Status)
      {
        case Status.Failed:
          failed++;
          break;
        case Status.Passed:
          passed++;
          break;
        case Status.Skipped:
          skipped++;
          break;
        case Status.NotRun:
          notRun++;
          break;
      }
    }
  }

  /// <summary>
  /// Adds the test case to the results, updating the count for its current status.
  /// </summary>
  /// <param name="testCase">The test case whose results are to be included.</param>
  public void Add([NotNull] in ITestCase testCase)
  {
    switch (testCase.Status)
    {
      case Status.Failed:
        failed++;
        break;
      case Status.Passed:
        passed++;
        break;
      case Status.Skipped:
        skipped++;
        break;
      case Status.NotRun:
        notRun++;
        break;
    }
  }
}
