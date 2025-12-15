using System;
using JetBrains.Annotations;

namespace DevTools.Testing;

[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method)]
internal class SmokeTestAttribute : Attribute
{
	public SmokeTestAttribute(TestType type)
	{
		Type = type;
	}

	public TestType Type { get; }
}