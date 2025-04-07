using System;
using System.Diagnostics;
using JetBrains.Annotations;

namespace DevTools
{
  public static class Assert
  {
    private static bool throwOnFail;

    [AssertionMethod]
    [Conditional("DEBUG"), Conditional("ASSERT_ENABLED")]
    public static void IsTrue([AssertionCondition(AssertionConditionType.IS_TRUE)] bool condition,
      string message = null)
    {
      if (condition)
        return;
      Fail($"Assert.IsTrue failed. {message}");
    }

    [AssertionMethod]
    [Conditional("DEBUG"), Conditional("ASSERT_ENABLED")]
    public static void IsFalse([AssertionCondition(AssertionConditionType.IS_FALSE)] bool condition,
      string message = null)
    {
      if (!condition)
        return;
      Fail($"Assert.IsFalse failed. {message}");
    }

    [AssertionMethod]
    [Conditional("DEBUG"), Conditional("ASSERT_ENABLED")]
    public static void IsNull<T>([AssertionCondition(AssertionConditionType.IS_NULL)] T obj,
      string message = null) where T : class
    {
      if (obj == null)
        return;
      Fail($"Assert.IsNull failed. {message}");
    }

    [AssertionMethod]
    [Conditional("DEBUG"), Conditional("ASSERT_ENABLED")]
    public static void IsNotNull<T>([AssertionCondition(AssertionConditionType.IS_NOT_NULL)] T obj,
      string message = null) where T : class
    {
      if (obj != null)
        return;
      Fail($"Assert.IsNotNull failed. {message}");
    }

    [Conditional("DEBUG"), Conditional("ASSERT_ENABLED")]
    public static void Fail(string message = null)
    {
      // NOTE - We don't need to insert the stack trace here, we'll be showing it in the assertion
      // popup and it will also be viewable via the message log window.
      if (Debugger.IsAttached) Debugger.Break();
      if (throwOnFail)
        throw new AssertFailException(message);
      Debug.ShowStack("Assertion Failed", message);
    }

    public readonly struct ThrowOnAssertEnabler : IDisposable
    {
      public ThrowOnAssertEnabler()
      {
        throwOnFail = true;
      }

      void IDisposable.Dispose()
      {
        throwOnFail = false;
      }
    }
  }
}