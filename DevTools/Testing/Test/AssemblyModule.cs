using System.Reflection;

namespace DevTools.Testing;

internal class AssemblyModule(Assembly assembly) : ITestModule
{
  string ITestModule.Name => assembly.GetName().Name;

  string ITestModule.Source => assembly.Location;
}
