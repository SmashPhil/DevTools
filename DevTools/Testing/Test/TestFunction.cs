using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using UnityEngine.Assertions;

namespace DevTools.Testing;

[DebuggerDisplay("Name = {Name}")]
internal class TestFunction : ITestFunction
{
  public TestFunction(ITestFixture fixture, MethodInfo method, MethodType methodType)
  {
    Fixture = fixture;
    MethodInfo = method;
    MethodType = methodType;
  }

  public ITestFixture Fixture { get; }

  public MethodType MethodType { get; }

  public ITestModule Module => Fixture.Module;

  public Type Type => MethodInfo.DeclaringType;

  public object[] Args { get; set; }

  public MetaDataContainer MetaData { get; } = new();

  public Status Status { get; set; }

  public MethodInfo MethodInfo { get; }

  public string Name => MethodInfo.Name;

  public object ExpectedResult { get; private set; }

  object ITestFunction.ExpectedResult { get => ExpectedResult; set => ExpectedResult = value; }

  public void Execute(object instance)
  {
    try
    {
      object result = MethodInfo.Invoke(instance, Args);
      if (ExpectedResult != null)
      {
        switch (ExpectedResult)
        {
          case float or double or decimal:
          {
            float a = Convert.ToSingle(ExpectedResult);
            float b = Convert.ToSingle(result);
            Assert.AreApproximatelyEqual(a, b);
            break;
          }
          default:
          {
            Assert.AreEqual(ExpectedResult, result);
            break;
          }
        }
      }
    }
    catch (Exception ex)
    {
      Test.Fail(ex);
    }
  }

  public IEnumerator ExecuteRoutine(object instance)
  {
    Assert.AreEqual(MethodInfo.ReturnType, typeof(IEnumerator));
    IEnumerator enumerator = (IEnumerator)MethodInfo.Invoke(instance, Args);
    while (true)
    {
      object current;
      try
      {
        if (!enumerator.MoveNext())
          break;

        current = enumerator.Current;
      }
      catch (Exception ex)
      {
        Test.Fail(ex);
        yield break;
      }
      yield return current;
    }
  }
}