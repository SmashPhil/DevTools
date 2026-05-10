namespace DevTools.Testing;

internal class TestPlanFixture : TestFixture
{
  private readonly TestPlanJob job;

  public TestPlanFixture(ITestModule module, in TestPlanJob job) :
    base(module, typeof(TestProcess), TestType.MainMenu)
  {
    Name = job.name;
    this.job = job;
  }

  public override string Name { get; }

  private PlanModule PlanModule => (PlanModule)Module;

  public override object CreateInstance()
  {
    TestProcess testProcess = (TestProcess)base.CreateInstance();
    testProcess.mod = PlanModule.Mod;
    testProcess.job = job;
    return testProcess;
  }
}