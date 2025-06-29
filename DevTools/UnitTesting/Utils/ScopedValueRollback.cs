using System;
using JetBrains.Annotations;

namespace DevTools.UnitTesting;

/// <summary>
/// Records the current value of an object and rolls it back when the object goes out of scope.
/// </summary>
/// <typeparam name="T">Type of the object being rolled back.</typeparam>
[PublicAPI]
public readonly unsafe struct ScopedValueRollback<T> : IDisposable where T : unmanaged
{
  private readonly T* ptr;
  private readonly T value;

  public ScopedValueRollback(ref T obj)
  {
    fixed (T* objPtr = &obj)
    {
      ptr = objPtr;
      value = obj;
    }
  }

  void IDisposable.Dispose()
  {
    *ptr = value;
  }
}