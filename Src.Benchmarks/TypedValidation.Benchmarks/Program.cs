using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using TypedValidation.Benchmarks;

var config = ManualConfig
    .Create(DefaultConfig.Instance)
    .WithSummaryStyle(SummaryStyle.Default.WithMaxParameterColumnWidth(50));

BenchmarkRunner.Run<ValidationBenchmarks>(config);
