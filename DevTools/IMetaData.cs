using JetBrains.Annotations;

namespace DevTools;

/// <summary>
/// Represents metadata supplied by an attribute and attached to a consuming member or type.
/// </summary>
/// <remarks>
/// This is intended for attribute-based metadata where an attribute exposes a key/value
/// pair that is collected and stored by <see cref="MetaDataContainer"/>
/// </remarks>
[PublicAPI]
public interface IMetaData
{
  /// <summary>
  /// Gets the identifier for this metadata entry.
  /// </summary>
  /// <remarks>
  /// Consumers can use this key to group, order, or look up metadata values.
  /// </remarks>
  int Key { get; }

  /// <summary>
  /// Gets the metadata value supplied by the implementing attribute.
  /// </summary>
  /// <remarks>
  /// The concrete type depends on the attribute implementation and how the metadata is consumed.
  /// </remarks>
  object Value { get; }
}