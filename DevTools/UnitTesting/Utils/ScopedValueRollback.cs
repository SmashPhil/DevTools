using System;
using JetBrains.Annotations;

namespace DevTools.UnitTesting;

/// <summary>
/// Captures the value of an unmanaged object and restores it when this object is disposed.
/// </summary>
/// <typeparam name="T">The unmanaged type of the object being rolled back.</typeparam>
/// <remarks>
/// This struct uses unsafe code to store a pointer to the target object and its original value.
/// On disposal, the original value is written back through the pointer.
/// </remarks>
[PublicAPI]
public readonly unsafe struct ScopedValueRollback<T> : IDisposable where T : unmanaged
{
  private readonly T* ptr;
  private readonly T value;

  /// <summary>
  /// Creates a new <see cref="ScopedValueRollback{T}"/>, capturing the current value of <paramref name="obj"/>.
  /// </summary>
  /// <param name="obj">The variable whose value will be temporarily saved.</param>
  public ScopedValueRollback(ref T obj)
  {
    fixed (T* objPtr = &obj)
    {
      ptr = objPtr;
      value = obj;
    }
  }

  /// <summary>
  /// Restores the original value back to the target object pointer.
  /// </summary>
  void IDisposable.Dispose()
  {
    *ptr = value;
  }
}