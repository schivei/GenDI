using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using GenDI.Benchmarks;

BenchmarkSwitcher
    .FromAssembly(typeof(StartupRegistrationBenchmarks).Assembly)
    .Run(args, DefaultConfig.Instance);
