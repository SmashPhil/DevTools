using System.Collections;
using System.Reflection;
using JetBrains.Annotations;

namespace DevTools.Testing;

/// <summary>
/// Test method entry backed by reflected method metadata.
/// </summary>
[PublicAPI]
public interface ITestFunction : ITestCase
{
  /// <summary>
  /// Test fixture this function belongs to.
  /// </summary>
  ITestFixture Fixture { get; }

  /// <summary>
	/// Classification of this method within the test lifecycle.
	/// </summary>
	MethodType MethodType { get; }

	/// <summary>
	/// Method metadata represented by this test function.
	/// </summary>
	MethodInfo MethodInfo { get; }

  /// <summary>
  /// The expected return value of this test function after being executed.
  /// </summary>
  object ExpectedResult { get; set; }

	/// <summary>
	/// Execute the test method on the provided fixture instance.
	/// </summary>
	/// <param name="instance">Object instance containing the test method.</param>
	void Execute(object instance);

	/// <summary>
	/// Execute the test method as a coroutine on the provided fixture instance.
	/// </summary>
	/// <param name="instance">Object instance containing the test method.</param>
	/// <returns>Enumerator used to drive coroutine-based test execution.</returns>
	IEnumerator ExecuteRoutine(object instance);
}