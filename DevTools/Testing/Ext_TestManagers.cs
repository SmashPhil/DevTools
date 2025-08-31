using System.IO;
using System.Reflection;
using JetBrains.Annotations;
using Verse;

namespace DevTools.Testing;

[PublicAPI]
public static class Ext_TestManagers
{
	public static void OpenLogFile(this ITestManager testManager)
	{
	}

	public static T LoadConfig<T>(this ITestManager testManager, ModContentPack mod) where T : ITestConfig, new()
	{
		const string ConfigFolderName = "Configs";
		const string ConfigFileExt = ".xml";

		T config = default;
		FileInfo file =
			new(GenFile.ResolveCaseInsensitiveFilePath(Path.Combine(mod.RootDir, ConfigFolderName),
				$"{testManager.ConfigName}{ConfigFileExt}"));
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

	public static void ClearTestResults(this ITestManager testManager)
	{
		// Parents should propagate their reset to containing functions / groups
		foreach (ITestGroup testGroup in testManager.TestGroups)
		{
			testGroup.Reset();
		}
	}

	public static TestRunner GetRunnerWith(this ITestManager testManager, [NotNull] TestFilter filter)
	{
		return new TestRunner(testManager, filter);
	}

	public static TestRunner GetRunnerWith(this ITestManager testManager, [NotNull] ExpressionTree expressionTree)
	{
		return new TestRunner(testManager, expressionTree);
	}

	public static TestRunner GetRunnerWith<T>(this ITestManager testManager, Expression.Comparison comparison,
		string value)
		where T : Expression, new()
	{
		return testManager.GetRunnerWith(new T(), comparison, value);
	}

	public static TestRunner GetRunnerWith(this ITestManager testManager, Expression expression,
		Expression.Comparison comparison,
		string value)
	{
		ExpressionTree tree = new();
		tree.Add(expression, comparison, value);
		return new TestRunner(testManager, tree);
	}

	internal static bool HasAllRequiredMods(this MemberInfo memberInfo)
	{
		if (memberInfo.TryGetAttribute<LoadIfModsActiveAttribute>() is { } loadIfModsActive &&
			!loadIfModsActive.PackageIds.NullOrEmpty())
		{
			foreach (string packageId in loadIfModsActive.PackageIds)
			{
				if (ModLister.GetActiveModWithIdentifier(packageId, ignorePostfix: true) is null)
					return false;
			}
		}
		return true;
	}

	internal static bool HasAnyRequiredMods(this MemberInfo memberInfo)
	{
		if (memberInfo.TryGetAttribute<LoadIfAnyModsActiveAttribute>() is { } loadIfAnyModActive &&
			!loadIfAnyModActive.PackageIds.NullOrEmpty())
		{
			foreach (string packageId in loadIfAnyModActive.PackageIds)
			{
				if (ModLister.GetActiveModWithIdentifier(packageId, ignorePostfix: true) != null)
					return true;
			}
			return false;
		}
		return true;
	}
}