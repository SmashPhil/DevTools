using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace DevTools.TestAdapter;

internal sealed class RunSettings
{
  private readonly IRunSettings settings;

  public RunSettings([NotNull] IRunSettings settings)
  {
    this.settings = settings;
    LoadFromXml();
  }

  public string PackageId { get; private set; }

  public string ExePath { get; private set; }

  public bool AutoRecacheOnBuild { get; private set; } = true;

  public WindowMode Mode { get; private set; } = WindowMode.NoWindow;

  private void LoadFromXml()
  {
    string xml = settings?.SettingsXml;
    if (string.IsNullOrWhiteSpace(xml))
      throw new InvalidOperationException("Run settings are required to launch RimWorld.");

    XElement parameters = XDocument.Parse(xml).Root?.Element("TestRunParameters");
    if (parameters != null)
    {
      foreach (XElement parameter in parameters.Elements("Parameter"))
      {
        switch (parameter.Attribute("name")?.Value)
        {
          case "DevTools.PackageId":
            PackageId = parameter.Attribute("value")?.Value.Trim();
            break;
          case "DevTools.RimWorldExePath":
            ExePath = parameter.Attribute("value")?.Value.Trim();
            break;
          case "DevTools.AutoRecacheOnBuild":
            if (!bool.TryParse(parameter.Attribute("value")?.Value, out bool autoRecacheOnBuild))
              throw new InvalidOperationException("DevTools.AutoRecacheOnBuild must be true or false.");
            AutoRecacheOnBuild = autoRecacheOnBuild;
            break;
          case "DevTools.WindowMode":
            if (!Enum.TryParse(parameter.Attribute("value")?.Value, out WindowMode mode))
            {
              throw new InvalidOperationException(
                $"DevTools.Mode must map to a valid enum value: {string.Join(", ", Enum.GetNames<WindowMode>())}");
            }
            Mode = mode;
            break;
        }
      }
    }

    if (string.IsNullOrWhiteSpace(PackageId))
      throw new InvalidOperationException("Missing DevTools.PackageId in run settings.");
    if (string.IsNullOrWhiteSpace(ExePath))
      throw new InvalidOperationException("Missing DevTools.RimWorldExePath in run settings.");

    ExePath = Environment.ExpandEnvironmentVariables(ExePath);
    if (!Path.IsPathFullyQualified(ExePath) || !File.Exists(ExePath))
      throw new FileNotFoundException("RimWorld executable not found.", ExePath);
  }

  public enum WindowMode
  {
    Windowed,
    Minimized,
    NoWindow,
    Headless
  }
}
