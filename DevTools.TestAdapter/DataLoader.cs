using System;
using System.IO;
using System.Text.Json;

namespace DevTools.TestAdapter;

internal static class DataLoader<T> where T : IVersionable
{
  private const string SchemaVersion = "1.0.0";
  private const string Folder = @"Ludeon Studios/RimWorld by Ludeon Studios/DevTools";

  private static string GetPath(string fileName)
  {
    string dirPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Folder);
    if (!Directory.Exists(Folder))
    {
      Directory.CreateDirectory(dirPath);
    }
    return Path.Combine(dirPath, fileName);
  }

  public static T Load(string fileName)
  {
    string dirPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Folder);
    if (!Directory.Exists(Folder))
    {
      Directory.CreateDirectory(dirPath);
    }
    string filePath = Path.Combine(dirPath, fileName);
    if (!File.Exists(filePath))
      return default;

    using FileStream stream = File.OpenRead(filePath);
    T data = JsonSerializer.Deserialize<T>(stream);
    if (data.Schema != SchemaVersion)
    {
      Console.Error.Write("Loading file with mismatched schema version.");
    }
    return data;
  }
}