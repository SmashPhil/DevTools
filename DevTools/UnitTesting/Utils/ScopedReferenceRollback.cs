using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine.Assertions;

namespace DevTools.UnitTesting;

/// <summary>
/// Temporarily overrides a reference‐type field on an object, and restores its original value when disposed.
/// </summary>
/// <typeparam name="T">The type of the object containing the field.</typeparam>
/// <typeparam name="F">The type of the field to override.</typeparam>
public readonly struct ScopedReferenceRollback<T, F> : IDisposable where T : class
{
  private static readonly Dictionary<FieldInfo, AccessTools.FieldRef<T, F>> FieldRefsByType = [];

  private readonly AccessTools.FieldRef<T, F> fieldRef;
  private readonly T instance;
  private readonly F oldValue;

  /// <summary>
  /// Captures the current value of the named field on <paramref name="instance"/>.
  /// The field will be restored to this value when this object goes out of scope.
  /// </summary>
  /// <param name="instance">The target object whose field will be overridden.</param>
  /// <param name="name">The name of the field to capture and later restore.</param>
  public ScopedReferenceRollback(T instance, string name)
  {
    this.instance = instance;
    this.fieldRef = GetFieldRef(typeof(T), name);
    this.oldValue = fieldRef(instance);
  }

  /// <summary>
  /// Captures the current value of the named field on <paramref name="instance"/>,
  /// then immediately sets it to <paramref name="newValue"/>.
  /// The original value will be restored when this object goes out of scope.
  /// </summary>
  /// <param name="instance">The target object whose field will be overridden.</param>
  /// <param name="name">The name of the field to capture and later restore.</param>
  /// <param name="newValue">The temporary value to assign to the field.</param>
  public ScopedReferenceRollback(T instance, string name, F newValue) : this(instance, name)
  {
    fieldRef(instance: instance) = newValue;
  }

  /// <summary>
  /// Restores the field on the original instance to its captured value.
  /// </summary>
  void IDisposable.Dispose()
  {
    fieldRef(instance) = oldValue;
  }

  /// <summary>
  /// Looks up and caches a <see cref="AccessTools.FieldRef&lt;T, F&gt;"/> for the given field name on the given type.
  /// </summary>
  /// <param name="type">The type declaring the target field.</param>
  /// <param name="name">The name of the field.</param>
  /// <returns>
  /// Returns a by‐ref access to the field of type <typeparamref name="F"/>.
  /// </returns>
  private static AccessTools.FieldRef<T, F> GetFieldRef(Type type, string name)
  {
    FieldInfo field = AccessTools.Field(type, name);
    Assert.IsNotNull(field);
    if (!FieldRefsByType.ContainsKey(field))
      FieldRefsByType.Add(field, AccessTools.FieldRefAccess<T, F>(field));
    return FieldRefsByType[field];
  }
}