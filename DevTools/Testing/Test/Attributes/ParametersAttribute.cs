using System;
using JetBrains.Annotations;

namespace DevTools.Testing;

[PublicAPI]
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class ParametersAttribute(params object[] args) : Attribute
{
	public object[] Arguments { get; } = args;
}
