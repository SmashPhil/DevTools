using System;
using JetBrains.Annotations;

namespace DevTools.Benchmarking;

[PublicAPI, MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method)]
public class PrepareAttribute : Attribute
{
}