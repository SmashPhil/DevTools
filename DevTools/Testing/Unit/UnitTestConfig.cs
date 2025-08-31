using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Verse;

namespace DevTools.Testing;

[PublicAPI]
public class UnitTestConfig : BaseTestConfig, ITestActions
{
	public bool stopOnFailure;
	public bool retryOnFailure;

	public bool showDisabledTests;
	public bool showSetUpAndTearDowns;

	public List<Action> preTestActions;
	public List<Action> postTestActions;

	public override bool StopOnFailure => stopOnFailure;

	public override int RetryAttempts => retryOnFailure ? 1 : 0;

	bool ITestActions.ShouldStop(ITestCase testCase)
	{
		return stopOnFailure && testCase.Status == Status.Failed;
	}

	bool ITestActions.PreTest(ITestGroup _)
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

	bool ITestActions.PostTest(ITestGroup _)
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