using System.Reflection;

namespace DevTools.Testing;

internal class AssemblyModule(Assembly assembly) : ITestModule
{
  public string Name => assembly.GetName().Name;
}
