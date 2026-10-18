using System;
using System.Threading;

namespace DevTools.TestAdapter;

internal readonly struct MutexLock : IDisposable
{
  private readonly bool taken;
  private readonly Mutex mutex;

  public MutexLock(Mutex mutex)
  {
    this.mutex = mutex;
    taken = mutex.WaitOne();
  }

  public void Dispose()
  {
    if (taken)
    {
      mutex.ReleaseMutex();
    }
  }
}