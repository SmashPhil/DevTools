using System;
using System.Collections.Generic;
using Verse;

namespace DevTools.Testing;

internal class TestPlanJob : ITestFixture
{
	public string name;
	public string commandLineArgs;
	public List<string> loadWithMods;
	public float timeOut = -1;

	[Unsaved]
	private TestContainer container;

	public TestType TestType => TestType.MainMenu;

	public string SaveFile => null;

	public IEnumerable<ITestFunction> TestFunctions
	{
		get { yield return container; }
	}

	public string Name => name;

	public Type Type => null;

  object[] ITestCase.Args { get; set; }

  public MetaDataContainer MetaData { get; } = new();

	public Status Status { get; set; } = Status.NotRun;

  public void PostLoadInit(ModContentPack mod)
	{
		container = new TestContainer(mod, this);
	}

  bool ITestFixture.OneTimeSetUp(object instance)
  {
    return true;
  }

  bool ITestFixture.OneTimeTearDown(object instance)
  {
    container.Dispose();
    Status = container.Status;
    return true;
  }

  bool ITestFixture.SetUp(object instance)
	{
		return true;
	}

	bool ITestFixture.TearDown(object instance)
	{
		return true;
	}
}