using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Verse;

namespace DevTools.Testing;

[PublicAPI]
public class TestFixtureConfig : BaseTestConfig, ITestActions
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