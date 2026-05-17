using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using GenDI.Benchmarks;

[assembly: GenDI.GenDICoveration(false)]

BenchmarkSwitcher
    .FromAssembly(typeof(StartupRegistrationBenchmarks).Assembly)
    .Run(args, DefaultConfig.Instance);
