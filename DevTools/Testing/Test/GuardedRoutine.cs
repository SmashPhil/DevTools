using System;
using System.Collections;
using System.Collections.Generic;

namespace DevTools.Testing;

internal static class GuardedRoutine
{
  /// <summary>
  /// Runs <paramref name="routine"/> and every <see cref="IEnumerator"/> it yields, including
  /// <see cref="UnityEngine.CustomYieldInstruction"/>s, on one stack so that an exception anywhere
  /// in the chain reaches <paramref name="onException"/> instead of the coroutine scheduler, which
  /// would stop the whole chain, runner included.
  /// </summary>
  /// <param name="routine">Coroutine to execute.</param>
  /// <param name="onException">Function to call if an exception is thrown. This function must not throw.</param>
  /// <remarks>
  /// A nested routine starts and resumes its parent within the same frame, as with the scheduler.
  /// After an exception the routines still suspended are disposed so their finally blocks run.
  /// </remarks>
  public static IEnumerator Run(IEnumerator routine, Action<Exception> onException)
  {
    Stack<IEnumerator> stack = new();
    using RoutineScope rs = new(stack, onException);
    stack.Push(routine);
    while (stack.Count > 0)
    {
      object current;
      try
      {
        if (!stack.Peek().MoveNext())
        {
          (stack.Pop() as IDisposable)?.Dispose();
          continue;
        }
        current = stack.Peek().Current;
      }
      catch (Exception ex)
      {
        onException(ex);
        yield break;
      }

      if (current is IEnumerator nested)
      {
        stack.Push(nested);
      }
      else
      {
        yield return current;
      }
    }
  }

  /// <summary>
  /// Guarantee cleanup of the routine stack when the guarded routine is terminated.
  /// </summary>
  private readonly struct RoutineScope(Stack<IEnumerator> stack, Action<Exception> onException) : IDisposable
  {
    public void Dispose()
    {
      while (stack.Count > 0)
      {
        try
        {
          (stack.Pop() as IDisposable)?.Dispose();
        }
        catch (Exception ex)
        {
          onException(ex);
        }
      }
    }
  }
}
