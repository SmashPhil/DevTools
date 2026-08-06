using System;
using JetBrains.Annotations;

namespace DevTools.Testing;

/// <summary>
/// Marks a method to run once after all tests in the fixture.
/// </summary>
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method)]
public class OneTimeTearDownAttribute : Attribute
{
}
