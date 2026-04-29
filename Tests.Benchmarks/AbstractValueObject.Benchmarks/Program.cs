using AbstractValueObject.Benchmarks;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

var config = ManualConfig
    .Create(DefaultConfig.Instance)
    .WithSummaryStyle(SummaryStyle.Default.WithMaxParameterColumnWidth(50));

BenchmarkRunner.Run(
[
    typeof(ArrayEqualityBenchmarks),
    typeof(ArrayHashCodeBenchmarks),
], config);
