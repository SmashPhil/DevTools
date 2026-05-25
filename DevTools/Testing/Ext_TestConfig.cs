using System;
using UnityEngine;

namespace DevTools.Testing;

internal static class Ext_TestConfig
{
	public static bool VerifyForLogType(this ITestConfig config, LogType logType)
	{
		return logType switch
		{
			// Assert and Exception included for verbosity.  The former should never occur as asserts
			// are designed to always throw. Non-throwing asserts are deprecated.
			LogType.Error or LogType.Assert or LogType.Exception => config.FailOnErrors,
			LogType.Warning                                      => config.FailOnWarnings,
			_                                                    => false
		};
	}

	public static bool LogContained(this ITestConfig config, LogType logType, string message)
	{
		return logType switch
		{
			LogType.Assert or LogType.Exception or LogType.Error or LogType.Warning =>
				config.SuppressLogFailure(logType, message),
			LogType.Log => false,
			_           => throw new NotImplementedException(nameof(LogType)),
		};
	}
}