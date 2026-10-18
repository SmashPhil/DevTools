using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace DevTools.TestAdapter;

[ExtensionUri(AdapterInfo.ExecutorUri)]
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

    if (runContext == null)
    {
      Console.Error.WriteLine("Cannot launch tests without RunContext.");
      return;
    }
    try
    {
      RunSettings settings = new(runContext.RunSettings);
      session = new TestSession(settings.PackageId, tempFile, settings.ExePath);
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
}
