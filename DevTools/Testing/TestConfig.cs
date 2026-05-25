using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using UnityEngine;
using Verse;

namespace DevTools.Testing;

[PublicAPI]
public sealed class TestConfig : ITestConfig, ITestActions
{
  public uint? randSeed;
  public WorldGenerationSettings world = new();
  public MapGenerationSettings map = MapGenerationSettings.Default;

  public bool failOnWarnings = true;
  public bool failOnErrors = true;

  public List<string> warningsAllowed;
  public List<string> errorsAllowed;

  public Logger.Config log = new();

  public bool stopOnFailure;
  public bool retryOnFailure;

  public List<Action> preTestActions;
  public List<Action> postTestActions;

  public bool StopOnFailure => stopOnFailure;

  public ushort RetryAttempts => (ushort)(retryOnFailure ? 1 : 0);

  Logger.Config ITestConfig.LogConfig => log;

  uint? ITestConfig.Seed => randSeed;

  WorldGenerationSettings ITestConfig.WorldSettings => world;

  MapGenerationSettings ITestConfig.MapSettings => map;

  bool ITestConfig.FailOnWarnings => failOnWarnings;

  bool ITestConfig.FailOnErrors => failOnErrors;

  private List<Regex> WarningRegexes { get; } = [];

  private List<Regex> ErrorRegexes { get; } = [];

  public void PostLoad()
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

  bool ITestConfig.SuppressLogFailure(LogType logType, string message)
  {
    switch (logType)
    {
      case LogType.Error or LogType.Assert or LogType.Exception:
        foreach (Regex regex in ErrorRegexes)
        {
          if (regex.IsMatch(message))
            return true;
        }
        break;
      case LogType.Warning:
        foreach (Regex regex in WarningRegexes)
        {
          if (regex.IsMatch(message))
            return true;
        }
        break;
    }
    return false;
  }

  bool ITestActions.ShouldStop(ITestCase testCase)
  {
    return stopOnFailure && testCase.Status == Status.Failed;
  }

  bool ITestActions.PreTest(ITestFixture _)
  {
    bool success = true;
    if (!preTestActions.NullOrEmpty())
    {
      foreach (Action action in preTestActions)
      {
        action();
        success &= Test.Current.Status is not Status.Failed;
      }
    }
    return success;
  }

  bool ITestActions.PostTest(ITestFixture _)
  {
    bool success = true;
    if (!postTestActions.NullOrEmpty())
    {
      foreach (Action action in postTestActions)
      {
        action();
        success &= Test.Current.Status is not Status.Failed;
      }
    }
    return success;
  }
}