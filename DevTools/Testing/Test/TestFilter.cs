using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace DevTools.Testing;

[PublicAPI]
public sealed class TestFilter
{
  private readonly List<FilterFunc> filters = [];

  public delegate bool FilterFunc(ITestFixture fixture, ITestFunction function);

  public TestFilter()
  {
  }

  public TestFilter(FilterFunc filter)
  {
    filters.Add(filter);
  }

  public IEnumerable<(ITestFixture, List<ITestFunction>)> MatchesFrom(IEnumerable<ITestFixture> fixtures)
  {
    var groups = fixtures
      .OrderBy(group => group, TestFixtureComparer.Default)
      .ThenBy(group => group, TestCaseComparer.Default)
      .Select(group => (group, group.TestFunctions));
    List<(ITestFixture, List<ITestFunction>)> tests = [];
    foreach ((ITestFixture fixture, IEnumerable<ITestFunction> functions) in groups)
    {
      List<ITestFunction> functionsToRun = [];
      foreach (ITestFunction function in functions)
      {
        if (Evaluate(fixture, function))
        {
          functionsToRun.Add(function);
        }
      }

      if (functionsToRun.Count > 0)
      {
        tests.Add((fixture, functionsToRun));
      }
    }
    return tests;
  }

  public bool Evaluate(ITestFixture fixture, ITestFunction function)
  {
    foreach (FilterFunc filter in filters)
    {
      if (!filter(fixture, function))
        return false;
    }
    return true;
  }

  public static TestFilter operator +(TestFilter a, TestFilter b)
  {
    ArgumentNullException.ThrowIfNull(a);
    ArgumentNullException.ThrowIfNull(b);
    a.filters.AddRange(b.filters);
    return a;
  }

  public static TestFilter operator +(TestFilter a, FilterFunc func)
  {
    ArgumentNullException.ThrowIfNull(a);
    ArgumentNullException.ThrowIfNull(func);
    a.filters.Add(func);
    return a;
  }

  public static implicit operator TestFilter(FilterFunc func)
  {
    return new TestFilter(func);
  }
}
