using System.Text.Json;

namespace DevTools.Testing;

internal interface IJsonSerializable
{
  JsonSerializerOptions Options { get; }

  string FileName { get; }
}
