using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

namespace DevTools.Testing;

[PublicAPI]
public static class Ext_TestManagers
{
  extension(ITestManager manager)
  {
    public void OpenLogFile()
    {
      string filePath = manager.Config?.LogConfig?.FullPath;
      if (filePath == null || !File.Exists(filePath))
      {
        Messages.Message("No log file to open.", MessageTypeDefOf.RejectInput);
        return;
      }
      Application.OpenURL(filePath);
    }

    public T LoadConfig<T>(ModContentPack mod) where T : ITestConfig, new()
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

    public void SaveMetaData()
    {
      const string SchemaVersion = "1.0.0";
      foreach (TestManifest manifest in GenerateManifests(manager))
      {
        DataWriter<TestManifest>.Serialize(manifest);
      }
      return;

      static IEnumerable<TestManifest> GenerateManifests(ITestManager manager)
      {
        Dictionary<string, TestManifest> manifestsByAsm = [];
        Dictionary<MemberInfo, int> caseNumbers = [];
        foreach (ITestFixture fixture in manager.TestFixtures)
        {
          string name = fixture.Type.Assembly.GetName().Name;
          if (string.IsNullOrEmpty(name))
            continue;

          if (!manifestsByAsm.TryGetValue(name, out TestManifest manifest))
          {
            manifest = new TestManifest
            {
              Assembly = name,
              Schema = SchemaVersion,
              Version = VersionControl.CurrentVersionStringWithRev,
              ModList = ModsConfig.ActiveModsInLoadOrder.Select(modMeta => modMeta.PackageIdNonUnique).ToArray(),
              Assemblies = LoadedModManager.RunningModsListForReading
                .SelectMany(modContent => modContent.assemblies.loadedAssemblies
                  .Select(asm => asm.FullName)).Order().ToArray(),
              Tests = []
            };
            manifestsByAsm[name] = manifest;
          }
          foreach (ITestFunction function in fixture.TestFunctions)
          {
            string suffix = "";
            if (function.Args is { Length: > 0 })
            {
              caseNumbers.TryGetValue(function.MethodInfo, out int caseNum);
              suffix = $"#case:{caseNum}";
              caseNumbers[function.MethodInfo] = ++caseNum;
            }
            manifest.Tests.Add(new TestManifest.TestMetaData
            {
              FullName = function.FullName,
              FullyQualifiedName = $"{function.FullName}{suffix}",
              Source = fixture.Type.Assembly.Location
            });
          }
        }

        foreach (TestManifest manifest in manifestsByAsm.Values)
        {
          manifest.Tests.SortBy(test => test.FullName);
          yield return manifest;
        }
      }
    }

    public TestRunner GetRunnerWith([NotNull] TestFilter filter)
    {
      return new TestRunner(manager, filter);
    }

    public TestRunner GetRunnerWith([NotNull] ExpressionTree expressionTree)
    {
      return new TestRunner(manager, expressionTree);
    }

    public TestRunner GetRunnerWith<T>(Expression.Comparison comparison, string value)
      where T : Expression, new()
    {
      return manager.GetRunnerWith(new T(), comparison, value);
    }

    public TestRunner GetRunnerWith(Expression expression,
      Expression.Comparison comparison,
      string value)
    {
      ExpressionTree tree = new();
      tree.Add(expression, comparison, value);
      return new TestRunner(manager, tree);
    }
  }

  public static TestPlan FindTestPlan(string name, ModContentPack mod)
  {
    var testManager = DevHarmony.GetDevTool<TestPlanManager>(mod);
    return testManager.FindPlan(name);
  }
}