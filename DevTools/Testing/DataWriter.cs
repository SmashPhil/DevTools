using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Verse;

namespace DevTools.Testing;

#pragma warning disable CS0649 // Field is never assigned to

internal sealed class TestManifest : IJsonSerializable
{
  public string Schema { get; set; }
  public string Version { get; set; }
  public string[] ModList { get; set; }
  public string[] Assemblies { get; set; }
  public List<TestMetaData> Tests { get; set; }

  [JsonIgnore]
  string IJsonSerializable.FileName => "LoadedTestCases.json";

  [JsonIgnore]
  JsonSerializerOptions IJsonSerializable.Options => new() { WriteIndented = true };

  public class TestMetaData
  {
    public string Key { get; set; }

    public string FullyQualifiedName { get; set; }

    public string Source { get; set; }
  }
}

internal static class DataWriter<T> where T : IJsonSerializable
{
  private const string Folder = @"Ludeon Studios\RimWorld by Ludeon Studios\DevTools";

  private static string GetPath(T serializable)
  {
    string dirPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Folder);
    if (!Directory.Exists(Folder))
    {
      Directory.CreateDirectory(dirPath);
    }
    return Path.Combine(dirPath, serializable.FileName);
  }

  public static void Serialize(T serializable)
  {
    string filePath = GetPath(serializable);
    using FileStream stream = File.OpenWrite(filePath);
    
    JsonSerializer.Serialize(stream, serializable, serializable.Options);
  }

  public static async Task SerializeAsync(T serializable)
  {
    try
    {
      string filePath = GetPath(serializable);
      await using FileStream stream = File.Create(filePath);
      await JsonSerializer.SerializeAsync(stream, serializable, serializable.Options);
    }
    catch (Exception ex)
    {
      Log.Error($"Exception thrown writing test metadata to cache.\nException={ex}");
    }
  }
}
