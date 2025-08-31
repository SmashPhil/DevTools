using System;

namespace DevTools.Testing;

[AttributeUsage(AttributeTargets.Method)]
public class TearDownAttribute : Attribute
{
}