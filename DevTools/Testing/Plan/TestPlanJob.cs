using System.Collections.Generic;
using JetBrains.Annotations;

namespace DevTools.Testing;

[PublicAPI]
public struct TestPlanJob
{
  public string name;
  public string commandLineArgs;
  public List<string> loadWithMods;
  public float timeOut;
}