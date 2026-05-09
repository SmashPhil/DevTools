using System;
using JetBrains.Annotations;

namespace DevTools.Testing;

/// <summary>
/// Test entry in explorer window. Contains info related to the test(s) being executed
/// </summary>
[PublicAPI]
public interface ITestCase
{
	/// <summary>
	/// Test name shown in test explorer
	/// </summary>
	string Name { get; }

	/// <summary>
	/// Class type containing this test case
	/// </summary>
	Type Type { get; }

	/// <summary>
	/// Final test status. Highest severity Status is preserved
	/// </summary>
	Status Status { get; set; }

	/// <summary>
	/// Meta data container for key/value lookups
	/// </summary>
	MetaDataContainer MetaData { get; }

  /// <summary>
  /// Parameters for this test
  /// </summary>
  object[] Args { get; set; }
}