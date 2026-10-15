using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Mono.Cecil;

namespace DevTools.TestAdapter;

[FileExtension(".dll")]
[DefaultExecutorUri(AdaptorInfo.ExecutorUri)]
public sealed class TestDiscoverer : ITestDiscoverer
{
  private const string TestCaseFile = "LoadedTestCases.json";

  void ITestDiscoverer.DiscoverTests(IEnumerable<string> sources,
    IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
  {
    AssemblyDefList defList = new(sources);
    var data = DataLoader<TestData>.Load(TestCaseFile);
    // TODO - Check for outdated / mismatched modlist
    if (data != null)
    {
      LoadFromData(data, discoverySink);
    }
    else
    {
      LoadFromDefinitions(defList, discoverySink);
    }
  }

  private static void LoadFromDefinitions(AssemblyDefList defList, ITestCaseDiscoverySink discoverySink)
  {
    foreach ((string path, AssemblyDefinition def) assembly in defList.assemblies)
    {
      foreach (TypeDefinition type in GetTypes(assembly.def.MainModule.Types))
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
            TestCase testCase = new($"{qualifiedName}{data.NameSuffix}", new Uri(AdaptorInfo.ExecutorUri), assembly.path)
            {
              DisplayName = data.DisplayName
            };
            discoverySink.SendTestCase(testCase);
          }
        }
      }
    }
  }

  private static void LoadFromData(TestData data, ITestCaseDiscoverySink discoverySink)
  {
    foreach (var metaData in data.Tests)
    {
      TestCase testCase = new(metaData.FullyQualifiedName, new Uri(AdaptorInfo.ExecutorUri), metaData.Source)
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

  private readonly struct TestNames(string displayName, string nameSuffix)
  {
    public string NameSuffix { get; } = nameSuffix;

    public string DisplayName { get; } = displayName;
  }

  private class AssemblyDefList : IDisposable
  {
    public readonly List<(string path, AssemblyDefinition def)> assemblies = [];

    public AssemblyDefList(IEnumerable<string> sources)
    {
      foreach (string path in sources)
      {
        var assembly = AssemblyDefinition.ReadAssembly(path);
        assemblies.Add((path, assembly));
      }
    }

    void IDisposable.Dispose()
    {
      foreach ((_, AssemblyDefinition def) in assemblies)
      {
        def.Dispose();
      }
      assemblies.Clear();
    }
  }
}
