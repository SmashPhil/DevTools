using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine.Assertions;
using Verse;

namespace DevTools.UnitTesting;

/// <summary>
/// GenSteps_Scatterer warns if they it fails to find a valid location to place the entity which
/// can lead to the test runner failing if the fail on warnings is enabled. On small maps, it's
/// almost guaranteed that it will not be able to find space. This should have no impact on tests,
/// so we can safely suppress these warnings by disabling warnOnFail.
/// </summary>
[PublicAPI]
public readonly struct GenStepWarningDisabler : IDisposable
{
  private static readonly Dictionary<GenStep_Scatterer, bool> previousSettings = [];

  public GenStepWarningDisabler()
  {
    Disable();
  }

  void IDisposable.Dispose()
  {
    Restore();
  }

  public static void Disable()
  {
    foreach (GenStepDef genStepDef in DefDatabase<GenStepDef>.AllDefsListForReading)
    {
      Assert.IsNotNull(genStepDef.genStep);
      if (genStepDef.genStep is GenStep_Scatterer scatterer)
      {
        previousSettings[scatterer] = scatterer.warnOnFail;
        scatterer.warnOnFail = false;
      }
    }
  }

  public static void Restore()
  {
    foreach ((GenStep_Scatterer scatterer, bool prevValue) in previousSettings)
    {
      scatterer.warnOnFail = prevValue;
    }
    previousSettings.Clear();
  }
}