using JetBrains.Annotations;

namespace DevTools.Testing;

/// <summary>
/// Test entry in explorer window. Contains info related to the test(s) being executed
/// </summary>
[PublicAPI]
public interface ITestCase
{
  /// <summary>
  /// Unique identifier used for test selection when running tests via CLI or adaptor.
  /// </summary>
  string Key { get; }

  /// <summary>
	/// Test name shown in test explorer
	/// </summary>
	string Name { get; }

  /// <summary>
  /// Test module this test case belongs to
  /// </summary>
  ITestModule Module { get; }

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