namespace DevTools.Benchmarking;

internal interface IRunner
{
  const int TargetMsPerBatch = 500;
  const int WarmUpIterations = 5;

  int Partitions { get; }

  Benchmark.Measurement Measurement { get; }

  Benchmark.Result Execute();
}