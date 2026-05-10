using System.IO;
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
    string fileName = DevHarmony.Args?.config ?? manager.ConfigName;
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