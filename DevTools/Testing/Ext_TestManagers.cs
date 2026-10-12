using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

namespace DevTools.Testing;

[PublicAPI]
public static class Ext_TestManagers
{
  public static void OpenLogFile(this ITestManager manager)
  {
    string filePath = manager.Config?.LogConfig?.FullPath;
    if (filePath == null || !File.Exists(filePath))
    {
      Messages.Message("No log file to open.", MessageTypeDefOf.RejectInput);
      return;
    }
    Application.OpenURL(filePath);
  }

  public static T LoadConfig<T>(this ITestManager manager, ModContentPack mod) where T : ITestConfig, new()
  {
    const string ConfigFolderName = "Configs";
    const string ConfigFileExt = ".xml";

    T config = default;
    string fileName = manager.ConfigName;
    string filePath = GenFile.ResolveCaseInsensitiveFilePath(Path.Combine(mod.RootDir, ConfigFolderName),
      $"{fileName}{ConfigFileExt}");
    FileInfo file = new(filePath);
    if (file.Exists)
    {
      config = DirectXmlLoader.ItemFromXmlFile<T>(file.FullName);
      config?.PostLoad();
      if (config == null)
        Log.Warning($"[{mod.PackageIdPlayerFacing}] Unable to load test config at {fileName}{ConfigFileExt}");
    }
    // Load defaults
    config ??= new T();
    return config;
  }

  public static void SaveMetaData(this ITestManager manager)
  {
    const string SchemaVersion = "1.0.0";
    Task.Run(async () =>
    {
      var manifest = GenerateManifest(manager);
      await DataWriter<TestManifest>.SerializeAsync(manifest);
    });
    return;

    static TestManifest GenerateManifest(ITestManager manager)
    {
      TestManifest manifest = new()
      {
        Schema = SchemaVersion,
        Version = VersionControl.CurrentVersionString,
        ModList = ModsConfig.ActiveModsInLoadOrder.Select(modMeta => modMeta.PackageIdNonUnique).ToArray(),
        Assemblies = LoadedModManager.RunningModsListForReading
          .SelectMany(modContent => modContent.assemblies.loadedAssemblies
            .Select(asm => asm.FullName)).Order().ToArray(),
        Tests = []
      };
      Dictionary<MemberInfo, int> caseNumbers = [];
      foreach (ITestFixture fixture in manager.TestFixtures)
      {
        foreach (ITestFunction function in fixture.TestFunctions)
        {
          string suffix = string.Empty;
          if (function.Args is { Length: > 0 })
          {
            caseNumbers.TryGetValue(function.MethodInfo, out int caseNum);
            suffix = $"#case:{caseNum}";
            caseNumbers[function.MethodInfo] = ++caseNum;
          }
          manifest.Tests.Add(new TestManifest.TestMetaData
          {
            Key = function.Key,
            FullyQualifiedName = $"{fixture.Key}.{function.MethodInfo.Name}{suffix}",
            Source = fixture.Type.Assembly.Location
          });
        }
      }
      manifest.Tests.SortBy(test => test.Key);
      return manifest;
    }
  }

  public static TestRunner GetRunnerWith(this ITestManager manager, [NotNull] TestFilter filter)
  {
    return new TestRunner(manager, filter);
  }

  public static TestRunner GetRunnerWith(this ITestManager manager, [NotNull] ExpressionTree expressionTree)
  {
    return new TestRunner(manager, expressionTree);
  }

  public static TestRunner GetRunnerWith<T>(this ITestManager manager, Expression.Comparison comparison,
    string value)
    where T : Expression, new()
  {
    return manager.GetRunnerWith(new T(), comparison, value);
  }

  public static TestRunner GetRunnerWith(this ITestManager manager, Expression expression,
    Expression.Comparison comparison,
    string value)
  {
    ExpressionTree tree = new();
    tree.Add(expression, comparison, value);
    return new TestRunner(manager, tree);
  }
}