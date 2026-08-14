using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using JetBrains.Annotations;

namespace DevTools.Testing;

/// <summary>
/// Generates a Markdown report for test results.
/// </summary>
[UsedImplicitly]
internal sealed class MarkdownReport : ITestReport
{
  private const string FileName = "test-report.md";

  private StreamWriter writer;

  void ITestReport.Tabulate(string directoryPath, List<ITestGroup> modules)
  {
    try
    {
      string filePath = Path.Combine(directoryPath, FileName);
      // NOTE: Encoding.UTF8 includes BOM marker.
      writer = new StreamWriter(filePath, append: false, Encoding.UTF8);
      WriteSummary(modules);
    }
    finally
    {
      writer?.Dispose();
      writer = null;
    }
  }

  private void WriteSummary(List<ITestGroup> modules)
  {
    int passed = 0, failed = 0, skipped = 0;
    double totalMilliseconds = 0;
    foreach (ITestGroup module in modules)
    {
      foreach (ITestGroup fixture in module.Children)
      {
        totalMilliseconds += fixture.Duration.Total;
        foreach (ITestGroup test in EnumerateTests(fixture))
        {
          switch (test.Status)
          {
            case Status.Passed:
              passed++;
              break;
            case Status.Failed:
              failed++;
              break;
            case Status.Skipped:
              skipped++;
              break;
          }
        }
      }
    }

    int completed = passed + failed + skipped;
    writer.WriteLine($"https://img.shields.io/badge/Tests-{passed}%20passed{(failed > 0 ? $"%2c%20{failed}%20failed" : "")}" +
                     $"-{(failed > 0 ? "red" : "success")}");
    writer.WriteLine();

    // Details
    writer.WriteLine("<details><summary>Expand for details</summary>");
    writer.WriteLine();
    writer.WriteLine($"{completed} tests were completed in {FormatDuration(totalMilliseconds)} with " +
                     $"{passed} passed, {failed} failed, and {skipped} skipped.");
    writer.WriteLine();
    for (int i = 0; i < modules.Count; i++)
    {
      WriteModule(modules[i], i);
    }
    WriteDetails(modules);
    writer.WriteLine("</details>");
  }

  private void WriteModule(ITestGroup module, int moduleIndex)
  {
    writer.WriteLine($"## {Encode(module.Label)}");
    writer.WriteLine();
    writer.WriteLine("| Fixture | Passed | Failed | Skipped | Time |");
    writer.WriteLine("| --- | ---: | ---: | ---: | ---: |");

    int fixtureIndex = 0;
    foreach (ITestGroup fixture in module.Children)
    {
      List<ITestGroup> tests = EnumerateTests(fixture).ToList();
      int passed = 0, failed = 0, skipped = 0;
      foreach (ITestGroup test in tests)
      {
        switch (test.Status)
        {
          case Status.Passed:
            passed++;
            break;
          case Status.Failed:
            failed++;
            break;
          case Status.Skipped:
            skipped++;
            break;
        }
      }
      string anchor = FixtureAnchor(moduleIndex, fixtureIndex++);
      writer.Write($"| <a href=\"#{anchor}\">{Encode(fixture.Label)}</a>");
      string passedStr = passed > 0 ? $"{passed} {StatusMarker(Status.Passed)}" : "";
      string failedStr = failed > 0 ? $"{failed} {StatusMarker(Status.Failed)}" : "";
      string skippedStr = skipped > 0 ? $"{skipped} {StatusMarker(Status.Skipped)}" : "";
      writer.WriteLine($" | {passedStr} | {failedStr} | {skippedStr} | {FormatDuration(fixture.Duration.Total)} |");
    }

    writer.WriteLine();
  }

  private void WriteDetails(IReadOnlyList<ITestGroup> modules)
  {
    writer.WriteLine("## Details");
    writer.WriteLine();
    for (int moduleIndex = 0; moduleIndex < modules.Count; moduleIndex++)
    {
      ITestGroup module = modules[moduleIndex];
      writer.WriteLine($"### {Encode(module.Label)}");
      writer.WriteLine();

      int fixtureIndex = 0;
      foreach (ITestGroup fixture in module.Children)
      {
        WriteFixtureDetails(fixture, FixtureAnchor(moduleIndex, fixtureIndex++));
      }
    }
  }

  private void WriteFixtureDetails(ITestGroup fixture, string anchor)
  {
    List<ITestGroup> tests = EnumerateTests(fixture).ToList();
    writer.WriteLine($"<a id=\"{anchor}\"></a>");
    writer.WriteLine();
    writer.WriteLine($"#### {Encode(fixture.Label)}");
    writer.WriteLine();

    if (fixture.Status is Status.Failed && tests.All(test => test.Status is not Status.Failed))
    {
      WriteFailureDetails(fixture);
      writer.WriteLine();
      writer.WriteLine();
    }

    writer.WriteLine("<ul>");
    foreach (ITestGroup test in tests)
    {
      writer.WriteLine($"<li>{StatusMarker(test.Status)} {Encode(test.Label)}" +
                       $" &mdash; <strong>{test.Status}</strong> ({FormatDuration(test.Duration.Total)})");
      if (test.Status is Status.Failed)
      {
        WriteFailureDetails(test);
      }
      writer.WriteLine("</li>");
    }
    writer.WriteLine("</ul>");
    writer.WriteLine();
  }

  private void WriteFailureDetails(ITestGroup group)
  {
    string failure = BuildFailureText(group);
    if (string.IsNullOrWhiteSpace(failure))
      return;

    string encodedFailure = Encode(failure)
      .Replace("\r\n", "&#10;")
      .Replace("\r", "&#10;")
      .Replace("\n", "&#10;");
    writer.WriteLine($"<pre>{encodedFailure}</pre>");
  }

  private static string BuildFailureText(ITestGroup group)
  {
    StringBuilder failure = new();
    AppendLine(group.TestContext);
    AppendLine(group.FailLabel);
    AppendLine(group.FailMessage);
    AppendLine(group.Exception?.ToString());
    AppendLine(group.StackTrace?.ToString());
    return failure.ToString().TrimEnd();

    void AppendLine(string value)
    {
      if (!string.IsNullOrWhiteSpace(value))
      {
        failure.AppendLine(value);
      }
    }
  }

  private static IEnumerable<ITestGroup> EnumerateTests(ITestGroup group)
  {
    if (group.TestCase is ITestFunction)
    {
      yield return group;
    }

    foreach (ITestGroup child in group.Children)
    {
      foreach (ITestGroup test in EnumerateTests(child))
      {
        yield return test;
      }
    }
  }

  private static string FormatDuration(double milliseconds)
  {
    return milliseconds > 1000
      ? $"{(milliseconds / 1000).ToString("0.00", CultureInfo.InvariantCulture)}s"
      : $"{milliseconds.ToString("0.00", CultureInfo.InvariantCulture)}ms";
  }

  private static string StatusMarker(Status status)
  {
    return status switch
    {
      Status.Passed => ":white_check_mark:",
      Status.Failed => ":x:",
      Status.Skipped => ":warning:",
      _ => string.Empty,
    };
  }

  private static string FixtureAnchor(int moduleIndex, int fixtureIndex)
  {
    return $"fixture-details-{moduleIndex}-{fixtureIndex}";
  }

  private static string Encode(string value)
  {
    // A raw pipe would terminate the outer Markdown table cell, even when nested inside HTML.
    return WebUtility.HtmlEncode(value).Replace("|", "&#124;");
  }
}
