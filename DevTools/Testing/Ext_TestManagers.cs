using System.IO;
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
			string filePath = manager.Config.LogConfig.FullPath;
			if (!File.Exists(filePath))
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
			FileInfo file =
				new(GenFile.ResolveCaseInsensitiveFilePath(Path.Combine(mod.RootDir, ConfigFolderName),
					$"{manager.ConfigName}{ConfigFileExt}"));
			if (file.Exists)
			{
				config = DirectXmlLoader.ItemFromXmlFile<T>(file.FullName);
				config?.PostLoad();
				if (config == null)
					Log.Warning($"[{mod.PackageIdPlayerFacing}] Unable to load test config.");
			}
			// Load defaults
			config ??= new T();
			return config;
		}

		public TestRunner GetRunnerWith([NotNull] TestFilter filter)
		{
			return new TestRunner(manager, filter);
		}

		public TestRunner GetRunnerWith([NotNull] ExpressionTree expressionTree)
		{
			return new TestRunner(manager, expressionTree);
		}

		public TestRunner GetRunnerWith<T>(Expression.Comparison comparison,
			string value)
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
}