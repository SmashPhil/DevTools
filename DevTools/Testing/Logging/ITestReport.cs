using System.Collections.Generic;
using JetBrains.Annotations;

namespace DevTools.Testing;

[PublicAPI]
public interface ITestReport
{
  void Tabulate(string directoryPath, List<ITestGroup> modules);
}
