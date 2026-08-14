using System.Collections.Generic;
using JetBrains.Annotations;

namespace DevTools.Testing;

/// <summary>
/// Enables report generation on test runner completion
/// </summary>
[PublicAPI]
public interface ITestReport
{
  /// <summary>
  /// Generate a report at the target <paramref name="directoryPath"/>
  /// </summary>
  /// <remarks>File name is not fixed, only the directory path should be adhered to.</remarks>
  /// <param name="directoryPath">Directory where the report will be generated.</param>
  /// <param name="modules">Test modules containing the completed results.</param>
  void Tabulate(string directoryPath, List<ITestGroup> modules);
}
