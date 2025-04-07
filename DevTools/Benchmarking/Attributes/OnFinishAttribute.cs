using System;

namespace DevTools.Benchmarking;

[AttributeUsage(AttributeTargets.Method)]
public class OnFinishAttribute : Attribute
{
}