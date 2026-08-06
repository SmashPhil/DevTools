using System;
using JetBrains.Annotations;

namespace DevTools.Testing;

/// <summary>
/// Marks a class as a test fixture.
/// </summary>
/// <param name="type">Type of tests contained in the fixture.</param>
[PublicAPI, MeansImplicitUse]
[AttributeUsage(AttributeTargets.Class)]
public class TestFixtureAttribute(TestType type) : Attribute
{
  /// <summary>
  /// Type of tests contained in the fixture.
  /// </summary>
  internal TestType Type { get; } = type;
}
