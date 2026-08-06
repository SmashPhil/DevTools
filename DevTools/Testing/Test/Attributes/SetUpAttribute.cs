using System;
using JetBrains.Annotations;

namespace DevTools.Testing;

/// <summary>
/// Marks a method to run before each test in the fixture.
/// </summary>
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method)]
public class SetUpAttribute : Attribute
{
}
