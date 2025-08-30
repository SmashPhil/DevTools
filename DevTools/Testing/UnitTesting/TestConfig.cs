using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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

  public uint? randSeed;
  public WorldGenerationSettings world = new();
  public MapGenerationSettings map = new();

  public Logger.Config log = new();
  public bool showDisabledTests;
  public bool showSetUpAndTearDowns;

  public List<Action> preTestActions;
  public List<Action> postTestActions;

  private List<Regex> WarningRegexes { get; } = [];

  private List<Regex> ErrorRegexes { get; } = [];

  internal void PostLoad()
  {
    if (!warningsAllowed.NullOrEmpty())
    {
      foreach (string regex in warningsAllowed)
      {
        WarningRegexes.Add(new Regex(regex, RegexOptions.Compiled));
      }
    }
    if (!errorsAllowed.NullOrEmpty())
    {
      foreach (string regex in errorsAllowed)
      {
        ErrorRegexes.Add(new Regex(regex, RegexOptions.Compiled));
      }
    }
  }

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
    switch (logType)
    {
      case LogType.Assert:
      case LogType.Exception:
      case LogType.Error:
        foreach (Regex regex in ErrorRegexes)
        {
          if (regex.IsMatch(message))
            return true;
        }
        return false;
      case LogType.Warning:
        foreach (Regex regex in WarningRegexes)
        {
          if (regex.IsMatch(message))
            return true;
        }
        return false;
      case LogType.Log:
        return false;
      default:
        throw new NotImplementedException(nameof(LogType));
    }
  }

  internal bool RunPreTests()
  {
    bool success = true;
    if (!preTestActions.NullOrEmpty())
    {
      foreach (Action action in preTestActions)
      {
        using Test.Group preTestGroup = new(action.Method.Name);
        action();
        success &= !Test.CurrentGroup.Results.Exists(FailedResult);
      }
    }
    return success;
  }

  internal bool RunPostTests()
  {
    bool success = true;
    if (!postTestActions.NullOrEmpty())
    {
      foreach (Action action in postTestActions)
      {
        using Test.Group postTestGroup = new(action.Method.Name);
        action();
        success &= !Test.CurrentGroup.Results.Exists(FailedResult);
      }
    }
    return success;
  }

  private static bool FailedResult(TestResult result)
  {
    return result.status == Status.Failed;
  }
}