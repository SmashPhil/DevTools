using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Mono.Cecil;

namespace DevTools.TestAdapter;

[FileExtension(".dll")]
[DefaultExecutorUri(AdapterInfo.ExecutorUri)]
public sealed class TestDiscoverer : ITestDiscoverer
{
  private const string TestCaseFile = "LoadedTestCases-{0}.json";
  private const string ModsConfigFile = "ModsConfig.xml";
  private const string MutexName = "DevToolsDiscoverer";

  private readonly Mutex mutex = new(initiallyOwned: false, name: MutexName);

  void ITestDiscoverer.DiscoverTests(IEnumerable<string> sources,
    IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
  {
    RunSettings settings = new(discoveryContext.RunSettings);

    foreach (string source in sources)
    {
      string sourceName = Path.GetFileNameWithoutExtension(source);
      TestData data = FetchTestData(sourceName, settings);
      if (data != null)
      {
        LoadFromData(data, discoverySink);
      }
      else
      {
        using var assemblyDef = AssemblyDefinition.ReadAssembly(source);
        LoadFromDefinitions(source, assemblyDef, discoverySink);
      }
    }
  }

  private TestData FetchTestData(string source, RunSettings settings)
  {
    TestData data;
    if (settings.AutoRecacheOnBuild)
    {
      using (new MutexLock(mutex))
      {
        string fileName = string.Format(TestCaseFile, source);
        data = DataLoader<TestData>.Load(fileName);
        if (settings.AutoRecacheOnBuild && (data is null || OutOfDateCache(data)))
        {
          //Console.WriteLine("Running RimWorld to recache test list.");
          //QuickBootGame(settings);
          //data = DataLoader<TestData>.Load(TestCaseFile);
        }
      }
    }
    else
    {
      data = DataLoader<TestData>.Load(TestCaseFile);
    }
    return data;
  }

  private static bool OutOfDateCache(TestData data)
  {
    string modsConfigPath = Path.Combine(GameFilePaths.SaveDataFolderPath, ModsConfigFile);
    FileInfo file = new(modsConfigPath);
    if (file is not { Exists: true })
    {
      Console.Error.WriteLine($"Unable to find {ModsConfigFile} at {modsConfigPath}");
      return false;
    }

    string xml = File.ReadAllText(file.FullName);
    XElement root = XDocument.Parse(xml).Root;
    bool isStale = root == null;
    if (root != null)
    {
      string version = root.Element("version")?.Value;
      isStale = version != data.Version;
      List<string> mods = root.Element("activeMods")?.Elements("li").Select(node => node.Value).ToList();
      isStale |= mods is null || data.ModList is null || mods.Count != data.ModList.Length;
      if (!isStale && mods is { Count: > 0 })
      {
        isStale = !data.ModList.SequenceEqual(mods);
      }
    }
    return isStale;
  }

  private static void LoadFromDefinitions(string path, AssemblyDefinition def, ITestCaseDiscoverySink discoverySink)
  {
    foreach (TypeDefinition type in GetTypes(def.MainModule.Types))
    {
      if (!type.IsClass || !HasAttribute(type, DevToolTypes.TestFixture))
        continue;

      foreach (MethodDefinition method in type.Methods)
      {
        // Cecil uses "/" for nested types, reflection uses "+"
        string typeName = type.FullName.Replace('/', '+');
        string qualifiedName = $"{typeName}.{method.Name}";
        foreach (TestNames data in AddTestsFrom(method))
        {
          TestCase testCase = new($"{qualifiedName}{data.NameSuffix}", new Uri(AdapterInfo.ExecutorUri), path)
          {
            DisplayName = data.DisplayName
          };
          discoverySink.SendTestCase(testCase);
        }
      }
    }
  }

  private static void LoadFromData(TestData data, ITestCaseDiscoverySink discoverySink)
  {
    foreach (var metaData in data.Tests)
    {
      TestCase testCase = new(metaData.FullyQualifiedName, new Uri(AdapterInfo.ExecutorUri), metaData.Source)
      {
        DisplayName = metaData.FullName
      };
      discoverySink.SendTestCase(testCase);
    }
  }

  private static IEnumerable<TestNames> AddTestsFrom(MethodDefinition method)
  {
    if (HasAttribute(method, DevToolTypes.Test))
    {
      foreach (TestNames data in AddTest(method))
      {
        yield return data;
      }
    }
    else if (HasAttribute(method, DevToolTypes.TestCase))
    {
      foreach (TestNames data in AddTestCase(method))
      {
        yield return data;
      }
    }
    yield break;

    static IEnumerable<TestNames> AddTest(MethodDefinition method)
    {
      if (method.Parameters is not { Count: 1 })
      {
        yield return new TestNames(method.Name, string.Empty);
      }
      else if (HasAttribute(method.Parameters[0], DevToolTypes.Parameters))
      {
        CustomAttribute attribute = GetAttribute(method.Parameters[0], DevToolTypes.Parameters);
        var args = (CustomAttributeArgument[])attribute.ConstructorArguments[0].Value;
        for (int i = 0; i < args.Length; i++)
        {
          yield return new TestNames($"{method.Name}({((CustomAttributeArgument)args[i].Value).Value})", $"#case:{i}");
        }
      }
    }

    static IEnumerable<TestNames> AddTestCase(MethodDefinition method)
    {
      for (int i = 0; i < method.CustomAttributes.Count; i++)
      {
        CustomAttribute attribute = method.CustomAttributes[i];
        if (attribute.AttributeType.FullName != DevToolTypes.TestCase)
          continue;

        var args = (CustomAttributeArgument[])attribute.ConstructorArguments[0].Value;
        string argString =
          args == null ? "null" : string.Join(", ", args.Select(FormatArgument));
        yield return new TestNames($"{method.Name}({argString})", $"#case:{i}");
      }
    }
  }

  private static CustomAttribute GetAttribute(ICustomAttributeProvider member, string attribute)
  {
    return member.CustomAttributes.FirstOrDefault(attr => attr.AttributeType.FullName == attribute);
  }

  private static bool HasAttribute(ICustomAttributeProvider member, string attribute)
  {
    bool matches = false;
    foreach (CustomAttribute attr in member.CustomAttributes)
    {
      string fullName = attr.AttributeType.FullName;
      if (fullName == DevToolTypes.Disabled)
        return false;

      matches |= fullName == attribute;
    }
    return matches;
  }

  private static IEnumerable<TypeDefinition> GetTypes(IEnumerable<TypeDefinition> types)
  {
    foreach (TypeDefinition type in types)
    {
      yield return type;

      foreach (TypeDefinition nested in GetTypes(type.NestedTypes))
      {
        yield return nested;
      }
    }
  }

  private static string FormatArgument(CustomAttributeArgument argument)
  {
    object value = argument.Value;

    // Cecil wraps boxed object arguments.
    if (value is CustomAttributeArgument boxed)
      return FormatArgument(boxed);

    if (value is CustomAttributeArgument[] array)
      return "[" + string.Join(", ", array.Select(FormatArgument)) + "]";

    if (value is string text)
      return "\"" + text.Replace("\\", @"\\").Replace("\"", "\\\"") + "\"";

    return Convert.ToString(value, CultureInfo.InvariantCulture) ?? "null";
  }

  private static void QuickBootGame(RunSettings settings)
  {
    ProcessStartInfo startInfo = new()
    {
      FileName = settings.ExePath,
      WorkingDirectory = Path.GetDirectoryName(settings.ExePath)!,
      UseShellExecute = true,
      CreateNoWindow = false,
      WindowStyle = ProcessWindowStyle.Normal
    };
    startInfo.ArgumentList.Add($"--test-discovery={settings.PackageId}");
    startInfo.ArgumentList.Add("-Headless");
    using Process process = Process.Start(startInfo);
    process?.WaitForExit();
  }

  private readonly struct TestNames(string displayName, string nameSuffix)
  {
    public string NameSuffix { get; } = nameSuffix;

    public string DisplayName { get; } = displayName;
  }
}
