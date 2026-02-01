using System;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;

namespace DevTools.Benchmarking;

internal sealed class TestHarness
{
  [UsedImplicitly]
  private readonly object instance;
  [UsedImplicitly]
	private readonly IntPtr funcPtr;

  private UnrolledBatch container;

  private delegate void UnrolledBatch(object instance);

  private enum MethodType
  {
    Instance,
    Static
  }

	private TestHarness(object instance, IntPtr funcPtr)
	{
		this.instance = instance;
		this.funcPtr = funcPtr;
	}

	public int UnrollFactor { get; private set; }

  public void Invoke()
  {
    container(instance);
  }

	public static TestHarness Create(object instance, IntPtr funcPtr, int unrollFactor)
	{
    TestHarness testHarness = new(instance, funcPtr);
    testHarness.container = testHarness.MakeDelegate(unrollFactor, MethodType.Instance);
    testHarness.UnrollFactor = unrollFactor;
    return testHarness;
  }

  public static TestHarness CreateStatic(IntPtr funcPtr, int unrollFactor)
  {
    TestHarness testHarness = new(null, funcPtr);
    testHarness.container = testHarness.MakeDelegate(unrollFactor, MethodType.Static);
    testHarness.UnrollFactor = unrollFactor;
    return testHarness;
  }

  public static TestHarness Create<R>(object instance, IntPtr funcPtr, int unrollFactor)
	{
		TestHarness testHarness = new(instance, funcPtr);
    testHarness.container = testHarness.MakeDelegate<R>(unrollFactor, MethodType.Instance);
    testHarness.UnrollFactor = unrollFactor;
		return testHarness;
	}

  public static TestHarness CreateStatic<R>(IntPtr funcPtr, int unrollFactor)
  {
    TestHarness testHarness = new(null, funcPtr);
    testHarness.container = testHarness.MakeDelegate<R>(unrollFactor, MethodType.Instance);
    testHarness.UnrollFactor = unrollFactor;
    return testHarness;
  }

  private UnrolledBatch MakeDelegate(int unrollFactor, MethodType methodType)
  {
    DynamicMethod method = new("TestHarness",
      typeof(void), [typeof(TestHarness), typeof(object)],
      typeof(TestHarness).Module,
      skipVisibility: true);

    FieldInfo funcPtrField = AccessTools.Field(typeof(TestHarness), nameof(funcPtr));

    ILGenerator ilg = method.GetILGenerator();

    ilg.DeclareLocal(typeof(IntPtr));

    ilg.Emit(OpCodes.Ldarg_0);
    ilg.Emit(OpCodes.Ldfld, funcPtrField);
    ilg.Emit(OpCodes.Stloc_0);

    Type[] paramerTypes = methodType is MethodType.Static ? [typeof(object)] : [];
    CallingConventions conv = methodType is MethodType.Static
      ? CallingConventions.Standard
      : CallingConventions.HasThis;
    for (int i = 0; i < unrollFactor; i++)
    {
      // instance.funcPtr()
      ilg.Emit(OpCodes.Ldarg_1);
      ilg.Emit(OpCodes.Ldloc_0);
      ilg.EmitCalli(OpCodes.Calli, 
        conv,
        returnType: typeof(void),
        paramerTypes,
        optionalParameterTypes: null);
    }

    ilg.Emit(OpCodes.Ret);

    return (UnrolledBatch)method.CreateDelegate(typeof(UnrolledBatch), target: this);
  }

  private UnrolledBatch MakeDelegate<R>(int unrollFactor, MethodType methodType)
  {
    DynamicMethod method = new("TestHarness",
      typeof(void), [typeof(TestHarness), typeof(object)],
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

    Type[] paramerTypes = methodType is MethodType.Static ? [typeof(object)] : [];
    CallingConventions conv = methodType is MethodType.Static
      ? CallingConventions.Standard
      : CallingConventions.HasThis;
    for (int i = 0; i < unrollFactor; i++)
    {
      // loc1 = this.funcPtr();
      ilg.Emit(OpCodes.Ldarg_1);
      ilg.Emit(OpCodes.Ldloc_0); // funcPtr
      ilg.EmitCalli(OpCodes.Calli, 
        conv,
        returnType: typeof(R),
        paramerTypes,
        optionalParameterTypes: null);

      ilg.Emit(OpCodes.Stloc_1);
    }

    // DeadCodeHelper.KeepAliveReadOnly(in result);
    ilg.Emit(OpCodes.Ldloca_S, 1);
    ilg.Emit(OpCodes.Call, deadCodeHelper);

    ilg.Emit(OpCodes.Ret);

    return (UnrolledBatch)method.CreateDelegate(typeof(UnrolledBatch), target: this);
  }
}