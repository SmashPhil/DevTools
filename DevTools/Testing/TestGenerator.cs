using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace DevTools.Testing;

internal class TestGenerator(ITestFixture fixture, MethodInfo methodInfo, MethodType methodType)
{
  public IEnumerable<ITestFunction> GenerateAll()
  {
    if (SetUpTestCase(out List<ITestFunction> functions))
    {
      foreach (ITestFunction function in functions)
      {
        if (!function.MetaData.Get<bool>(MetaDataName.Disabled))
        {
          yield return function;
        }
      }
      yield break;
    }
    List<object[]> args = TestExtensions.ExtractParameters(methodInfo);
    if (args.Count == 0)
    {
      TestFunction function = new(fixture, methodInfo, methodType);
      function.MetaData.Load(methodInfo);
      if (!function.MetaData.Get<bool>(MetaDataName.Disabled))
      {
        function.Args = [];
        yield return function;
      }
      yield break;
    }

    foreach (object[] arg in args)
    {
      TestFunction function = new(fixture, methodInfo, methodType);
      function.MetaData.Load(methodInfo);
      if (function.MetaData.Get<bool>(MetaDataName.Disabled))
        continue;

      function.Args = arg;
      yield return function;
    }
  }

  private bool SetUpTestCase(out List<ITestFunction> functions)
  {
    var attributes = methodInfo.GetCustomAttributes<TestCaseAttribute>().ToList();
    if (attributes.Count > 0)
    {
      functions = [];
      foreach (TestCaseAttribute attr in attributes)
      {
        if (attr.Arguments == null)
        {
          Log.Error($"Missing arguments for test case {methodInfo.Name}");
          continue;
        }
        ITestFunction function = new TestFunction(fixture, methodInfo, methodType);
        function.MetaData.Load(methodInfo);
        function.Args = [.. attr.Arguments];
        function.ExpectedResult = attr.ExpectedResult;
        functions.Add(function);
      }
      return true;
    }
    functions = null;
    return false;
  }
}
