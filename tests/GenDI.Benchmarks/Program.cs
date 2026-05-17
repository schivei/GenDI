using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using GenDI.Benchmarks;

[assembly: GenDI.GenDiCoveration(false)]

BenchmarkSwitcher
    .FromAssembly(typeof(StartupRegistrationBenchmarks).Assembly)
    .Run(args, DefaultConfig.Instance);
