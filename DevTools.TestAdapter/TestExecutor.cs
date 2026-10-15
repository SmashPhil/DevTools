using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace DevTools.TestAdapter;

[ExtensionUri(AdaptorInfo.ExecutorUri)]
internal sealed class TestExecutor : ITestExecutor2
{
  private const string TestListFileName = "DevToolTestList.txt";

  private TestSession session;

  void ITestExecutor.RunTests(IEnumerable<TestCase> tests, IRunContext runContext,
    IFrameworkHandle frameworkHandle)
  {
    if (tests == null)
      return;

    // ReSharper disable PossibleMultipleEnumeration
    string tempFile = Path.Combine(Path.GetTempPath(), TestListFileName);
    using (var stream = File.CreateText(tempFile))
    {
      foreach (TestCase test in tests)
      {
        stream.WriteLine(test.DisplayName);
      }
    }
    try
    {
      (string packageId, string exePath) = ResolveTestTarget(runContext);
      session = new TestSession(packageId, tempFile, exePath);
      session.Run(tests, frameworkHandle);
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
    throw new NotImplementedException();
  }

  bool ITestExecutor2.ShouldAttachToTestHost(IEnumerable<string> sources, IRunContext runContext)
  {
    return false;
  }

  bool ITestExecutor2.ShouldAttachToTestHost(IEnumerable<TestCase> tests, IRunContext runContext)
  {
    return false;
  }

  void ITestExecutor.Cancel()
  {
    session?.Stop();
  }

  private static (string PackageId, string ExePath) ResolveTestTarget(IRunContext runContext)
  {
    string xml = runContext?.RunSettings?.SettingsXml;
    if (string.IsNullOrWhiteSpace(xml))
      throw new InvalidOperationException("Run settings are required to launch RimWorld.");

    XElement parameters = XDocument.Parse(xml).Root?.Element("TestRunParameters");
    string packageId = null;
    string exePath = null;
    if (parameters != null)
    {
      foreach (XElement parameter in parameters.Elements("Parameter"))
      {
        switch (parameter.Attribute("name")?.Value)
        {
          case "DevTools.PackageId":
            packageId = parameter.Attribute("value")?.Value.Trim();
            break;
          case "DevTools.RimWorldExePath":
            exePath = parameter.Attribute("value")?.Value.Trim();
            break;
        }
      }
    }

    if (string.IsNullOrWhiteSpace(packageId))
      throw new InvalidOperationException("Missing DevTools.PackageId in run settings.");
    if (string.IsNullOrWhiteSpace(exePath))
      throw new InvalidOperationException("Missing DevTools.RimWorldExePath in run settings.");

    exePath = Environment.ExpandEnvironmentVariables(exePath);
    if (!Path.IsPathFullyQualified(exePath) || !File.Exists(exePath))
      throw new FileNotFoundException("RimWorld executable not found.", exePath);

    return (packageId, exePath);
  }
}
