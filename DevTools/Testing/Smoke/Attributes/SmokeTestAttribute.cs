using System;

namespace DevTools.Testing;

[AttributeUsage(AttributeTargets.Method)]
internal class SmokeTestAttribute : Attribute
{
	public SmokeTestAttribute(TestType type)
	{
		Type = type;
	}

	public TestType Type { get; }
}