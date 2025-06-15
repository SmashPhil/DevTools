using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace DevTools.UnitTesting;

public interface ITestFunction : ITestCase
{
  MethodType MethodType { get; }
  MethodInfo MethodInfo { get; }
  void Execute();
  IEnumerator ExecuteRoutine();
}