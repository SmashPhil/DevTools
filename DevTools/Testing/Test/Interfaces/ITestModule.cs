using JetBrains.Annotations;

namespace DevTools.Testing;

[PublicAPI]
public interface ITestModule
{
  string Name { get; }
}
