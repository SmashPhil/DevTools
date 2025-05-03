using System.IO;
using System.Reflection;
using UnityEngine;
using Verse;

namespace DevTools;

internal static class EmbeddedResourceLoader
{
  private static byte[] LoadBytes(string resourceName)
  {
    if (!UnityData.IsInMainThread)
    {
      Log.Error($"Tried to load embedded resource {resourceName} outside of main thread.");
      return null;
    }

    Assembly assembly = typeof(EmbeddedResourceLoader).Assembly;
    using Stream stream = assembly.GetManifestResourceStream(resourceName);
    if (stream == null)
    {
      Log.Error($"Unable to load {resourceName} from resource stream.");
      return null;
    }
    using MemoryStream memStream = new();
    stream.CopyTo(memStream);
    byte[] bytes = memStream.ToArray();
    return bytes;
  }

  public static Texture2D LoadTexture(string resourceName)
  {
    byte[] bytes = LoadBytes(resourceName);
    if (bytes == null)
      return null;

    Texture2D tex = new(2, 2, TextureFormat.Alpha8, false);
    tex.LoadImage(bytes);
    tex.name = resourceName;
    tex.filterMode = FilterMode.Bilinear;
    tex.anisoLevel = 1;
    tex.Apply(false, true);
    return tex;
  }
}