using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.CsProj;

var config = DefaultConfig.Instance
    .WithOptions(ConfigOptions.DisableOptimizationsValidator)
    .WithBuildTimeout(TimeSpan.FromMinutes(10));

BenchmarkSwitcher
    .FromAssembly(typeof(Program).Assembly)
    .Run(args, config);
