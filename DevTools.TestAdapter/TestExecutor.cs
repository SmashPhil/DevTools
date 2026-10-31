using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace DevTools.TestAdapter;

[ExtensionUri(AdapterInfo.ExecutorUri)]
internal sealed class TestExecutor : ITestExecutor2
{
  private static readonly TestProperty categoryProperty = RegisterFilterProperty("Category");
  private static readonly TestProperty classProperty = RegisterFilterProperty("Class");
  private static readonly TestProperty methodProperty = RegisterFilterProperty("Method");
  private static readonly TestProperty moduleProperty = RegisterFilterProperty("Module");
  private static readonly TestProperty namespaceProperty = RegisterFilterProperty("Namespace");
  private static readonly TestProperty statusProperty = RegisterFilterProperty("Status");
  private static readonly TestProperty typeProperty = RegisterFilterProperty("Type");

  private TestSession session;

  void ITestExecutor.RunTests(IEnumerable<TestCase> tests, IRunContext runContext,
    IFrameworkHandle frameworkHandle)
  {
    if (tests == null)
      return;

    if (runContext == null)
    {
      Console.Error.WriteLine("Cannot launch tests without RunContext.");
      return;
    }

    try
    {
      RunSettings settings = new(runContext.RunSettings);
      session = new TestSession(settings, runContext);
      session.LoadTests(tests);
      session.Run(frameworkHandle);
    }
    finally
    {
      session?.Dispose();
      session = null;
    }
  }

  void ITestExecutor.RunTests(IEnumerable<string> sources, IRunContext runContext,
    IFrameworkHandle frameworkHandle)
  {
    if (sources == null)
      return;

    if (runContext == null)
    {
      Console.Error.WriteLine("Cannot launch tests without RunContext.");
      return;
    }

    try
    {
      RunSettings settings = new(runContext.RunSettings);
      session = new TestSession(settings, runContext);
      string filter = string.Join(" || ", sources.Select(source =>
        $"Module == {Path.GetFileNameWithoutExtension(source)}"));

      ITestCaseFilterExpression dotnetFilter = runContext.GetTestCaseFilter(
        SupportedFilters(), GetPropertyFromString);
      if (!string.IsNullOrWhiteSpace(dotnetFilter?.TestCaseFilterValue))
      {
        filter = $"({filter}) && ({dotnetFilter.TestCaseFilterValue})";
      }

      session.AddArg($" --where \"{filter.Replace("\"", "\\\"")}\" ");
      session.Run(frameworkHandle);
    }
    finally
    {
      session?.Dispose();
      session = null;
    }
  }

  bool ITestExecutor2.ShouldAttachToTestHost(IEnumerable<string> sources, IRunContext runContext)
  {
    // Debug mode attaches to the RimWorld process
    return false;
  }

  bool ITestExecutor2.ShouldAttachToTestHost(IEnumerable<TestCase> tests, IRunContext runContext)
  {
    // Debug mode attaches to the RimWorld process
    return false;
  }

  void ITestExecutor.Cancel()
  {
    session?.Stop();
  }

  private static IEnumerable<string> SupportedFilters()
  {
    yield return "Category";
    yield return "Class";
    yield return "FullyQualifiedName";
    yield return "Method";
    yield return "Module";
    yield return "Name";
    yield return "Namespace";
    yield return "Status";
    yield return "Type";
  }

  private static TestProperty GetPropertyFromString(string name)
  {
    return name?.ToLowerInvariant() switch
    {
      "category" => categoryProperty,
      "class" => classProperty,
      "fullyqualifiedname" => TestCaseProperties.FullyQualifiedName,
      "method" => methodProperty,
      "module" => moduleProperty,
      "name" => TestCaseProperties.DisplayName,
      "namespace" => namespaceProperty,
      "status" => statusProperty,
      "type" => typeProperty,
      _ => null
    };
  }

  private static TestProperty RegisterFilterProperty(string name)
  {
    return TestProperty.Register($"DevTools.{name}", name, typeof(string), typeof(TestCase));
  }
}
