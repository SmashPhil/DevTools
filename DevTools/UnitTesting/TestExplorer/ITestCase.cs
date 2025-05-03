using System.Collections.Generic;
using System.Text;
using DevTools.Benchmarking;

namespace DevTools.UnitTesting;

internal interface ITestCase
{
  string Name { get; }
  int TestCount { get; }
  Benchmark.Result Duration { get; }
  Status Status { get; }
  string FailLabel { get; }
  string FailMessage { get; }
  MetaDataContainer MetaData { get; }
  void TestOutcomes(StatusCount statusCount);
}