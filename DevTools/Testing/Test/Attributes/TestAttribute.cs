using System;
using JetBrains.Annotations;

namespace DevTools.Testing;

/// <summary>
/// Marks a method as a test.
/// </summary>
[PublicAPI, MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method)]
public class TestAttribute : Attribute
{
}
