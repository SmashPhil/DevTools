using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Verse;

namespace DevTools.UnitTesting;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal class TestBlock
{
  public TestType type;
  public List<string> tests;
  public GenerationTemplate template;

  public List<UnitTestGroup> UnitTests { get; } = [];

  public bool TryDoPostLoad(UnitTestManager unitTestManager)
  {
    foreach (string text in tests)
    {
      if (!unitTestManager.TryGetUnitTest(text, out UnitTestGroup testGroup))
      {
        Type classType = GenTypes.GetTypeInAnyAssembly(text);
        if (classType is null)
        {
          Log.Error($"Unable to find type {text} for unit test.");
          continue;
        }
        if (!unitTestManager.TryGetUnitTest(classType.Name, out testGroup))
        {
          Log.Error($"Unable to match type name {classType.Name} to unit test.");
          continue;
        }
      }
      UnitTests.Add(testGroup);
    }
    return true;
  }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal class TestPlan
{
  public string name;
  public List<TestBlock> plan;

  public bool TryDoPostLoad(UnitTestManager unitTestManager)
  {
    if (plan.NullOrEmpty())
    {
      Log.Error($"{name} test plan is empty, removing from test manager...");
      return false;
    }

    foreach (TestBlock testBlock in plan)
    {
      if (!testBlock.TryDoPostLoad(unitTestManager))
      {
        return false;
      }
    }
    return true;
  }
}