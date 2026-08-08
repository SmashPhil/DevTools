using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace DevTools.Testing;

internal class SmokeTestFixture : ITestFixture
{
    private readonly List<TestFunction> tests = [];

    public SmokeTestFixture(ITestModule module, Type type, TestType testType)
    {
        Module = module;
        Type = type;
        TestType = testType;
        Args = [];
    }

    public string Name => TestType.ToString();

    string ITestFixture.SaveFile => null;

    public object[] Args { get; set; }

    public IEnumerable<ITestFunction> TestFunctions => tests;

    public MetaDataContainer MetaData { get; } = new();

    public TestType TestType { get; }

    public ITestModule Module { get; }

    public Type Type { get; }

    public Status Status { get; set; } = Status.NotRun;

    bool ITestFixture.OneTimeSetUp(object instance)
    {
        // TODO - prep map
        return true;
    }

    bool ITestFixture.OneTimeTearDown(object instance)
    {
        // TODO - prep map, lear test area
        return true;
    }

    bool ITestFixture.SetUp(object instance)
    {
        // TODO - spawn entity
        return true;
    }

    bool ITestFixture.TearDown(object instance)
    {
        // TODO - despawn entity
        return true;
    }

    object ITestFixture.CreateInstance()
    {
        return this.CreateTestClass();
    }

    public bool TryAddFunction(MethodInfo methodInfo)
    {
        if (!MethodIsSafe(methodInfo, out string reason))
        {
            Log.Error($"Unable to add {methodInfo.Name} to smoke test. {reason}");
            return false;
        }

        if (methodInfo.MissingRequiredMods())
            return false;

        TestFunction method = new(this, methodInfo, MethodType.Test);
        method.MetaData.Load(methodInfo);
        method.Args = [];
        tests.Add(method);
        return true;
    }

    private static bool MethodIsSafe(MethodInfo method, out string reason)
    {
        reason = null;
        if (method.ReturnType != typeof(void) && method.ReturnType != typeof(IEnumerator))
        {
            reason = "Return type must be IEnumerator or void.";
            return false;
        }

        ParameterInfo[] parameters = method.GetParameters();
        if (parameters.Length > 0)
        {
            reason = "Smoke test methods must not have any parameters";
            return false;
        }

        return true;
    }
}