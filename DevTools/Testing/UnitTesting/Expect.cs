using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine.Assertions.Comparers;
using Verse;

namespace DevTools.UnitTesting;

public static class Expect
{
	internal const string FailedLabel = "[Failed]";
	private const string CanceledLabel = "[Canceled]";
	private const string SkippedLabel = "[Skipped]";
	private const string PassedLabel = "[Passed]";
	private const string PendingLabel = "[Pending]";
	private const string NotRunLabel = "[NotRun]";

	public static void That(Func<bool> validator, string message = null)
	{
		Signal(validator(), nameof(That), message);
	}

	public static void IsTrue(bool condition, string message = null)
	{
		Signal(condition, nameof(IsTrue), message, MessageBuilder.BooleanFailureMessage(condition));
	}

	public static void IsFalse(bool condition, string message = null)
	{
		Signal(!condition, nameof(IsFalse), message, MessageBuilder.BooleanFailureMessage(condition));
	}

	public static void IsEmpty<T>(IEnumerable<T> collection, string message = null)
	{
		Signal(!collection.Any(), nameof(IsEmpty), message);
	}

	public static void IsNotEmpty<T>(IEnumerable<T> collection, string message = null)
	{
		Signal(collection.Any(), nameof(IsNotEmpty), message);
	}

	public static void All<T>(IEnumerable<T> items, Func<T, bool> validator, string message = null)
	{
		Signal(items.All(validator), nameof(All), message);
	}

	public static void Any<T>(IEnumerable<T> items, Func<T, bool> validator, string message = null)
	{
		Signal(items.Any(validator), nameof(Any), message);
	}

	public static void None<T>(IEnumerable<T> items, Func<T, bool> validator, string message = null)
	{
		Signal(!items.Any(validator), nameof(None), message);
	}

	public static void AreApproximatelyEqual(float lhs, float rhs, string message = null)
	{
		Signal(FloatComparer.s_ComparerWithDefaultTolerance.Equals(lhs, rhs),
			nameof(AreApproximatelyEqual), message,
			MessageBuilder.ComparisonFailureMessage(lhs, rhs));
	}

	public static void AreNotApproximatelyEqual(float lhs, float rhs, string message = null)
	{
		Signal(!FloatComparer.s_ComparerWithDefaultTolerance.Equals(lhs, rhs),
			nameof(AreNotApproximatelyEqual), message,
			MessageBuilder.ComparisonFailureMessage(lhs, rhs));
	}

	public static void ReferencesAreEqual<T>(T lhs, T rhs, string message = null) where T : class
	{
		Signal(ReferenceEquals(lhs, rhs), nameof(ReferencesAreEqual), message);
	}

	public static void ReferencesAreNotEqual<T>(T lhs, T rhs, string message = null) where T : class
	{
		Signal(!ReferenceEquals(lhs, rhs), nameof(ReferencesAreNotEqual), message);
	}

	public static void AreEqual<T>(T lhs, T rhs, string message = null)
	{
		Signal(EqualityComparer<T>.Default.Equals(lhs, rhs), nameof(AreEqual), message,
			MessageBuilder.ComparisonFailureMessage(lhs, rhs));
	}

	public static void AreNotEqual<T>(T lhs, T rhs, string message = null)
	{
		Signal(!EqualityComparer<T>.Default.Equals(lhs, rhs), nameof(AreNotEqual), message,
			MessageBuilder.ComparisonFailureMessage(lhs, rhs));
	}

	public static void GreaterThan<T>(T lhs, T rhs, string message = null) where T : IComparable
	{
		Signal(lhs.CompareTo(rhs) > 0, nameof(GreaterThan), message,
			MessageBuilder.ComparisonFailureMessage(lhs, rhs));
	}

	public static void GreaterThanOrEqualTo<T>(T lhs, T rhs, string message = null)
		where T : IComparable
	{
		Signal(lhs.CompareTo(rhs) > 0 || EqualityComparer<T>.Default.Equals(lhs, rhs),
			nameof(GreaterThanOrEqualTo), message,
			MessageBuilder.ComparisonFailureMessage(lhs, rhs));
	}

	public static void LessThan<T>(T lhs, T rhs, string message = null) where T : IComparable
	{
		Signal(lhs.CompareTo(rhs) < 0, nameof(LessThan), message,
			MessageBuilder.ComparisonFailureMessage(lhs, rhs));
	}

	public static void LessThanOrEqualTo<T>(T lhs, T rhs, string message = null) where T : IComparable
	{
		Signal(lhs.CompareTo(rhs) < 0 || EqualityComparer<T>.Default.Equals(lhs, rhs),
			nameof(LessThanOrEqualTo), message,
			MessageBuilder.ComparisonFailureMessage(lhs, rhs));
	}

	public static void IsNull<T>(T obj, string message = null) where T : class
	{
		Signal(obj == null, nameof(IsNull), message);
	}

	public static void IsNotNull<T>(T obj, string message = null) where T : class
	{
		Signal(obj != null, nameof(IsNotNull), message);
	}

	[DebuggerHidden]
	public static T Throws<T>(Action action, string message = null) where T : Exception
	{
		T exception = null;
		try
		{
			action();
		}
		catch (T ex)
		{
			exception = ex;
		}
		Signal(exception != null, nameof(Throws), message);
		return exception;
	}

	private static void Signal(bool result, string label, string message,
		string failureMessage = null)
	{
		SendSignal(result ? Status.Passed : Status.Failed, label, message, failureMessage,
			skipFrames: 3);
	}

	internal static void SendSignal(Status status, string label, string message,
		string failureMessage = null, int skipFrames = 1)
	{
		if (!UnitTestManager.RunningUnitTests)
		{
			Log.Error(
				"Using Expect outside of test watcher. Use Assert instead, Expect is exclusively for unit testing.");
			return;
		}

		if (status == Status.Failed)
			DevLog.Write(StatusMessage(status, label, message, failureMessage));
		else
			DevLog.WriteVerbose(StatusMessage(status, label, message, failureMessage));

		StackFrame stackFrame = null;
		if (status is Status.Skipped or Status.Canceled or Status.Failed)
		{
			StackTrace stackTrace = new(skipFrames, true);
			stackFrame = stackTrace.GetFrame(0);
			if (status is Status.Failed && Debugger.IsAttached && UnitTestManager.BreakOnTestFailure)
				Debugger.Break();
			DevLog.Write(stackTrace.ToString());
		}
		Test.CurrentGroup.Results.Add(new TestResult(status, label, message, stackFrame));
	}

	internal static string StatusMessage(Status status, string label, string message,
		string failureMessage = "")
	{
		return (status switch
		{
			Status.Failed   => $"{FailedLabel}     {label}    {message}    {failureMessage}",
			Status.Canceled => $"{CanceledLabel}   {label}    {message}    {failureMessage}",
			Status.Skipped  => $"{SkippedLabel}    {label}    {message}    {failureMessage}",
			Status.Passed   => $"{PassedLabel}     {label}    {message}    {failureMessage}",
			Status.Pending  => $"{PendingLabel}    {label}    {message}    {failureMessage}",
			Status.NotRun   => $"{NotRunLabel}     {label}    {message}    {failureMessage}",
			_               => throw new NotImplementedException(nameof(Status)),
		}).TrimEnd();
	}
}