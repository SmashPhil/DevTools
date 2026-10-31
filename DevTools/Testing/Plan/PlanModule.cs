using Verse;

namespace DevTools.Testing;

internal class PlanModule(ModContentPack mod, TestPlan plan) : ITestModule
{
  public string Name => plan.name;

  string ITestModule.Source => plan.FilePath;

  public ModContentPack Mod => mod;
}
