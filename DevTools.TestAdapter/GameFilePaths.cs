using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace DevTools.TestAdapter;

internal static partial class GameFilePaths
{
  private const string SaveFolderArg = "savedatafolder";
  private const string CompanyName = "Ludeon Studios";
  private const string ProductName = "RimWorld by Ludeon Studios";
  private static readonly Guid LocalLowId = new("A520A1A4-1780-4FF6-BD18-167343C5AF16");

  public static string SaveDataFolderPath
  {
    get
    {
      if (TryGetCommandLineArg(SaveFolderArg, out string filePath))
      {
        filePath = Path.TrimEndingDirectorySeparator(filePath);
        if (string.IsNullOrEmpty(filePath))
        {
          filePath = Path.DirectorySeparatorChar.ToString();
        }
      }
      else
      {
        filePath = GetDefaultSaveDataFolderPath();
      }

      return Path.Combine(filePath, "Config");
    }
  }

  private static string GetDefaultSaveDataFolderPath()
  {
    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
    {
      return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "Library", "Application Support", "RimWorld");
    }

    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    {
      string configHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
      if (string.IsNullOrEmpty(configHome))
      {
        configHome = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
          ".config");
      }
      return Path.Combine(configHome, "unity3d", CompanyName, ProductName);
    }

    return Path.Combine(GetLocalAppDataLow(), CompanyName, ProductName);
  }

  private static string GetLocalAppDataLow()
  {
    IntPtr pathPtr = IntPtr.Zero;
    try
    {
      if (SHGetKnownFolderPath(in LocalLowId, 0, IntPtr.Zero, out pathPtr) == 0)
      {
        return Marshal.PtrToStringUni(pathPtr)!;
      }
    }
    finally
    {
      Marshal.FreeCoTaskMem(pathPtr);
    }

    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
      "AppData", "LocalLow");
  }

  // GenCommandLine's format, so we need to parse it in the same way
  private static bool TryGetCommandLineArg(string key, out string value)
  {
    string[] commandLineArgs = Environment.GetCommandLineArgs();
    foreach (string arg in commandLineArgs)
    {
      if (arg.Contains('='))
      {
        string[] argPair = arg.Split('=');
        if (argPair.Length != 2)
          continue;

        if (string.Compare(argPair[0], key, ignoreCase: true, CultureInfo.InvariantCulture) == 0 ||
            string.Compare(argPair[0], "-" + key, ignoreCase: true, CultureInfo.InvariantCulture) == 0)
        {
          value = argPair[1];
          return true;
        }
      }
    }
    value = null;
    return false;
  }

  // ReSharper disable once InconsistentNaming
  [LibraryImport("shell32.dll")]
  private static partial int SHGetKnownFolderPath(in Guid rfid, uint dwFlags, IntPtr hToken, out IntPtr pszPath);
}
