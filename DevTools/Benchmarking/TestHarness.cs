using System;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;

namespace DevTools.Benchmarking;

internal sealed class TestHarness
{
	[UsedImplicitly]
	private readonly IntPtr funcPtr;

	private Action container;

	private TestHarness(IntPtr funcPtr)
	{
		this.funcPtr = funcPtr;
	}

	public int UnrollFactor { get; private set; }

	public void Invoke()
	{
		container();
	}

	public static TestHarness Create(IntPtr funcPtr, int unrollFactor)
	{
		DynamicMethod method = new("TestHarness",
			typeof(void), [typeof(TestHarness)],
			typeof(TestHarness).Module,
			skipVisibility: true);

		FieldInfo funcPtrField = AccessTools.Field(typeof(TestHarness), nameof(funcPtr));

		ILGenerator ilg = method.GetILGenerator();

		ilg.DeclareLocal(typeof(IntPtr));

		ilg.Emit(OpCodes.Ldarg_0);
		ilg.Emit(OpCodes.Ldfld, funcPtrField);
		ilg.Emit(OpCodes.Stloc_0);

		for (int i = 0; i < unrollFactor; i++)
		{
			// funcPtr()
			ilg.Emit(OpCodes.Ldloc_0); // funcPtr
			ilg.EmitCalli(OpCodes.Calli,
				CallingConventions.Standard,
				returnType: typeof(void),
				parameterTypes: [],
				optionalParameterTypes: null);
		}

		ilg.Emit(OpCodes.Ret);

		TestHarness testHarness = new(funcPtr);
		testHarness.container = (Action)method.CreateDelegate(typeof(Action), testHarness);
		testHarness.UnrollFactor = unrollFactor;
		return testHarness;
	}

	public static TestHarness Create<R>(IntPtr funcPtr, int unrollFactor)
	{
		DynamicMethod method = new("TestHarness",
			typeof(void), [typeof(TestHarness)],
			typeof(TestHarness).Module,
			skipVisibility: true);

		FieldInfo funcPtrField = AccessTools.Field(typeof(TestHarness), nameof(funcPtr));
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
			// funcPtr()
			ilg.Emit(OpCodes.Ldloc_0); // funcPtr
			ilg.EmitCalli(OpCodes.Calli,
				CallingConventions.Standard,
				returnType: typeof(R),
				parameterTypes: [],
				optionalParameterTypes: null);

			// DeadCodeHelper.KeepAliveReadOnly(in result);
			ilg.Emit(OpCodes.Stloc_1);
			ilg.Emit(OpCodes.Ldloca_S, 1);
			ilg.Emit(OpCodes.Call, deadCodeHelper);
		}

		ilg.Emit(OpCodes.Ret);

		TestHarness testHarness = new(funcPtr);
		testHarness.container = (Action)method.CreateDelegate(typeof(Action), testHarness);
		testHarness.UnrollFactor = unrollFactor;
		return testHarness;
	}
}