using System.Collections.Generic;

namespace DevTools.Testing;

internal class TestFixtureComparer : IComparer<ITestFixture>
{
  public static TestFixtureComparer Default { get; } = new();

  public int Compare(ITestFixture a, ITestFixture b)
  {
    if (b is null)
      return a is null ? 0 : -1;

    if (a is null)
      return 1;

    return a.TestType.CompareTo(b.TestType);
  }
}
