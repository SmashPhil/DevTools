using System;
using JetBrains.Annotations;

namespace DevTools.Testing;

/// <summary>
/// Marks a method to run once before all tests in the fixture.
/// </summary>
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method)]
public class OneTimeSetUpAttribute : Attribute
{
}
