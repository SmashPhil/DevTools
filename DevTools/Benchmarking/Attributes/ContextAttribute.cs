using System;

namespace DevTools.Benchmarking;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class ContextAttribute : Attribute
{
}