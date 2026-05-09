using System;
using JetBrains.Annotations;

namespace DevTools.Testing;

[PublicAPI, MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method)]
public class TestAttribute : Attribute
{
}