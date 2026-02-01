using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using JetBrains.Annotations;

namespace DevTools.Benchmarking;

[PublicAPI]
public static class DeadCodeHelper
{
	private static readonly HashSet<Type> SupportedTypes =
	[
		typeof(byte), typeof(sbyte),
		typeof(short), typeof(ushort),
		typeof(int), typeof(uint),
		typeof(bool), typeof(char),
		typeof(float), typeof(double),
		typeof(long), typeof(ulong),
		typeof(object)
	];

	private static volatile byte byteHolder;
	private static volatile sbyte sbyteHolder;
	private static volatile short shortHolder;
	private static volatile ushort ushortHolder;
	private static volatile int intHolder;
	private static volatile uint uintHolder;
	private static volatile bool boolHolder;
	private static volatile char charHolder;
	private static volatile float floatHolder;
	private static double doubleHolder;
	private static long longHolder;
	private static ulong ulongHolder;
	private static volatile object objectHolder;

	public static bool IsTypeSupported<T>()
	{
		return SupportedTypes.Contains(typeof(T));
	}

	// ReSharper disable ArrangeMethodOrOperatorBody

	// Typed
	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void Consume(byte value) => byteHolder = value;

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void Consume(sbyte value) => sbyteHolder = value;

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void Consume(short value) => shortHolder = value;

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void Consume(ushort value) => ushortHolder = value;

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void Consume(int value) => intHolder = value;

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void Consume(uint value) => uintHolder = value;

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void Consume(bool value) => boolHolder = value;

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void Consume(char value) => charHolder = value;

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void Consume(float value) => floatHolder = value;

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void Consume(double value) => Volatile.Write(ref doubleHolder, value);

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void Consume(long value) => Volatile.Write(ref longHolder, value);

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void Consume(ulong value) => Volatile.Write(ref ulongHolder, value);

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void Consume(object value)
	{
		// Write volatile field to prevent dead code elimination
		objectHolder = value;
		objectHolder = null;
	}

	// ReSharper restore ArrangeMethodOrOperatorBody


	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void KeepAlive<T>(T value)
	{
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void KeepAlive<T>(ref T value)
	{
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void KeepAliveReadOnly<T>(in T value)
	{
	}
}