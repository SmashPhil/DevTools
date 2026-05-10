using System;
using JetBrains.Annotations;

namespace DevTools.Testing;

[PublicAPI]
[AttributeUsage(AttributeTargets.Class)]
[Obsolete("Use [TestFixture] instead.")]
public class UnitTestAttribute : TestFixtureAttribute
{
  public UnitTestAttribute(TestType type) : base(type)
  {
  }
}