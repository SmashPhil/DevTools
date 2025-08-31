using System.Collections;
using System.Reflection;

namespace DevTools.Testing;

public interface ITestFunction : ITestCase
{
	MethodType MethodType { get; }
	MethodInfo MethodInfo { get; }
	void Execute();
	IEnumerator ExecuteRoutine();
}