using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using Verse;

namespace DevTools.UnitTesting;

[PublicAPI]
public class TestConfig
{
  public bool failOnWarnings = true;
  public bool failOnErrors = true;

  public List<string> warningsAllowed;
  public List<string> errorsAllowed;

  public bool stopOnFailure;
  public bool retryOnFailure;

  public int? randSeed;

  public Logger.Config log = new();
  public bool showDisabledTests;
  public bool showSetUpAndTearDowns;

  public List<Action> preTestActions;
  public List<Action> postTestActions;

  internal bool VerifyForLogType(LogType logType)
  {
    return logType switch
    {
      // Assert and Exception included for verbosity.  The former should never occur as asserts
      // are designed to always throw. Non-throwing asserts are deprecated.
      LogType.Error or LogType.Assert or LogType.Exception => failOnErrors,
      LogType.Warning                                      => failOnWarnings,
      _                                                    => false
    };
  }

  internal bool LogContained(LogType logType, string message)
  {
    return logType switch
    {
      LogType.Error   => errorsAllowed.NotNullAndContains(message),
      LogType.Warning => warningsAllowed.NotNullAndContains(message),
      _               => false
    };
  }

  internal void RunPreTests()
  {
    if (!preTestActions.NullOrEmpty())
    {
      foreach (Action action in preTestActions)
      {
        action();
      }
    }
  }

  internal void RunPostTests()
  {
    if (!postTestActions.NullOrEmpty())
    {
      foreach (Action action in postTestActions)
      {
        action();
      }
    }
  }
}