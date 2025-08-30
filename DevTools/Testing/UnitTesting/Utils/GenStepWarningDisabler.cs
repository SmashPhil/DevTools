using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine.Assertions;
using Verse;

namespace DevTools.UnitTesting;

/// <summary>
/// Suppresses warnings emitted by <see cref="GenStep_Scatterer"/>'s <c>warnOnFail</c> flag during small-map
/// test setups, preventing test-run failures when scatterer cannot find valid placement locations.
/// </summary>
/// <remarks>Restores original settings when disposed.</remarks>
[PublicAPI]
public readonly struct GenStepWarningDisabler : IDisposable
{
  /// <summary>
  /// Tracks previous <see cref="GenStep_Scatterer.warnOnFail"/> values
  /// for each scatterer instance.
  /// </summary>
  private static readonly Dictionary<GenStep_Scatterer, bool> PreviousSettings = [];

  /// <summary>
  /// Disables all <see cref="GenStep_Scatterer.warnOnFail"/> warnings.
  /// </summary>
  public GenStepWarningDisabler()
  {
    Disable();
  }

  /// <summary>
  /// Restores each <see cref="GenStep_Scatterer.warnOnFail"/> setting
  /// to its original value.
  /// </summary>
  void IDisposable.Dispose()
  {
    Restore();
  }

  /// <summary>
  /// Iterates through all <see cref="GenStepDef"/> definitions,
  /// suppressing warnings for each <see cref="GenStep_Scatterer"/>
  /// by setting <c>warnOnFail = false</c>, and records the prior state.
  /// </summary>
  public static void Disable()
  {
    foreach (GenStepDef genStepDef in DefDatabase<GenStepDef>.AllDefsListForReading)
    {
      Assert.IsNotNull(genStepDef.genStep);
      if (genStepDef.genStep is GenStep_Scatterer scatterer)
      {
        PreviousSettings[scatterer] = scatterer.warnOnFail;
        scatterer.warnOnFail = false;
      }
    }
  }

  /// <summary>
  /// Restores the <c>warnOnFail</c> flag on each <see cref="GenStep_Scatterer"/>
  /// instance to the value recorded before <see cref="Disable"/> was called,
  /// then clears the record.
  /// </summary>
  public static void Restore()
  {
    foreach ((GenStep_Scatterer scatterer, bool prevValue) in PreviousSettings)
    {
      scatterer.warnOnFail = prevValue;
    }
    PreviousSettings.Clear();
  }
}