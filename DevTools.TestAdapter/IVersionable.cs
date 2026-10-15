namespace DevTools.TestAdapter;

internal interface IVersionable
{
  string Schema { get; }
  string Version { get; }
  string[] ModList { get; }
}