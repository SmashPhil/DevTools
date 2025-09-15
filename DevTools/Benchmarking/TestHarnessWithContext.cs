using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using JetBrains.Annotations;

namespace DevTools.Benchmarking;

internal sealed class TestHarnessWithContext<C>
{
	[UsedImplicitly]
	public readonly IntPtr funcPtr;

	private ActionWithContext container;

	private delegate void ActionWithContext(ref C context);

	private TestHarnessWithContext(IntPtr funcPtr)
	{
		this.funcPtr = funcPtr;
	}

	public int UnrollFactor { get; private set; }

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Invoke(ref C context)
	{
		container(ref context);
	}

	public static TestHarnessWithContext<C> Create(IntPtr funcPtr, int unrollFactor)
	{
		DynamicMethod method = new("TestHarness",
			typeof(void), [typeof(TestHarnessWithContext<C>), typeof(C).MakeByRefType()],
			typeof(TestHarnessWithContext<C>).Module,
			skipVisibility: true);

		FieldInfo funcPtrField = AccessTools.Field(typeof(TestHarnessWithContext<C>), nameof(funcPtr));

		ILGenerator ilg = method.GetILGenerator();

		ilg.DeclareLocal(typeof(IntPtr));

		ilg.Emit(OpCodes.Ldarg_0);
		ilg.Emit(OpCodes.Ldfld, funcPtrField);
		ilg.Emit(OpCodes.Stloc_0);

		for (int i = 0; i < unrollFactor; i++)
		{
			// funcPtr(in context)
			ilg.Emit(OpCodes.Ldarg_1); // ref C context
			ilg.Emit(OpCodes.Ldloc_0); // funcPtr
			ilg.EmitCalli(OpCodes.Calli,
				CallingConventions.Standard,
				returnType: typeof(void),
				parameterTypes: [typeof(C).MakeByRefType()],
				optionalParameterTypes: null);
		}

		ilg.Emit(OpCodes.Ret);

		TestHarnessWithContext<C> testHarness = new(funcPtr);
		testHarness.container = (ActionWithContext)method.CreateDelegate(typeof(ActionWithContext), testHarness);
		testHarness.UnrollFactor = unrollFactor;
		return testHarness;
	}

	public static TestHarnessWithContext<C> Create<R>(IntPtr funcPtr, int unrollFactor)
	{
		DynamicMethod method = new("TestHarness",
			typeof(void), [typeof(TestHarnessWithContext<C>), typeof(C).MakeByRefType()],
			typeof(TestHarnessWithContext<C>).Module,
			skipVisibility: true);

		FieldInfo funcPtrField = AccessTools.Field(typeof(TestHarnessWithContext<C>), nameof(funcPtr));
		MethodInfo deadCodeHelper = AccessTools.Method(typeof(DeadCodeHelper), nameof(DeadCodeHelper.KeepAliveReadOnly),
			generics: [typeof(R)]);

		ILGenerator ilg = method.GetILGenerator();

		ilg.DeclareLocal(typeof(IntPtr));
		ilg.DeclareLocal(typeof(R));

		ilg.Emit(OpCodes.Ldarg_0);
		ilg.Emit(OpCodes.Ldfld, funcPtrField);
		ilg.Emit(OpCodes.Stloc_0);

		for (int i = 0; i < unrollFactor; i++)
		{
			// funcPtr(in context)
			ilg.Emit(OpCodes.Ldarg_1); // ref C context
			ilg.Emit(OpCodes.Ldloc_0); // funcPtr
			ilg.EmitCalli(OpCodes.Calli,
				CallingConventions.Standard,
				returnType: typeof(R),
				parameterTypes: [typeof(C).MakeByRefType()],
				optionalParameterTypes: null);

			// DeadCodeHelper.KeepAliveReadOnly(in result);
			ilg.Emit(OpCodes.Stloc_1);
			ilg.Emit(OpCodes.Ldloca_S, 1);
			ilg.Emit(OpCodes.Call, deadCodeHelper);
		}

		ilg.Emit(OpCodes.Ret);

		TestHarnessWithContext<C> testHarness = new(funcPtr);
		testHarness.container = (ActionWithContext)method.CreateDelegate(typeof(ActionWithContext), testHarness);
		testHarness.UnrollFactor = unrollFactor;
		return testHarness;
	}
}