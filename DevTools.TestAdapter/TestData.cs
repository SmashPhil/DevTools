using System.Collections.Generic;

namespace DevTools.TestAdapter;

internal sealed class TestData : IVersionable
{
  public string Schema { get; set; }
  public string Version { get; set; }
  public string[] ModList { get; set; }
  public string[] Assemblies { get; set; }
  public List<TestMetaData> Tests { get; set; }

  public class TestMetaData
  {
    public string FullName { get; set; }

    public string FullyQualifiedName { get; set; }

    public string Source { get; set; }
  }
}
