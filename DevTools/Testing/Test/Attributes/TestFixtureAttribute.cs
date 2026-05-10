using System;
using JetBrains.Annotations;

namespace DevTools.Testing;

[PublicAPI, MeansImplicitUse]
[AttributeUsage(AttributeTargets.Class)]
public class TestFixtureAttribute(TestType type) : Attribute
{
	public TestType Type { get; } = type;
}
