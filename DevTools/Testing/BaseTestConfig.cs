using System.Collections.Generic;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using UnityEngine;
using Verse;

namespace DevTools.Testing;

[PublicAPI]
public abstract class BaseTestConfig : ITestConfig
{
	public uint? randSeed;
	public WorldGenerationSettings world = new();
	public MapGenerationSettings map = MapGenerationSettings.Default;

	public bool failOnWarnings = true;
	public bool failOnErrors = true;

	public List<string> warningsAllowed;
	public List<string> errorsAllowed;

	public Logger.Config log = new();

	public abstract bool StopOnFailure { get; }

	public abstract int RetryAttempts { get; }

	Logger.Config ITestConfig.LogConfig => log;

	uint? ITestConfig.Seed => randSeed;

	WorldGenerationSettings ITestConfig.WorldSettings => world;

	MapGenerationSettings ITestConfig.MapSettings => map;

	bool ITestConfig.FailOnWarnings => failOnWarnings;

	bool ITestConfig.FailOnErrors => failOnErrors;

	private List<Regex> WarningRegexes { get; } = [];

	private List<Regex> ErrorRegexes { get; } = [];

	public virtual void PostLoad()
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

	bool ITestConfig.SupressLogFailure(LogType logType, string message)
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
}