# GenDI Benchmark Results

This document tracks Phase 4 performance validation for startup registration.

## Benchmark project

- Project: `tests/GenDI.Benchmarks`
- Tooling: BenchmarkDotNet (`0.15.6`)
- Focus: compare **generated registration** against **reflection-based registration scanning**.

### Run command

```bash
dotnet run -c Release --project tests/GenDI.Benchmarks/GenDI.Benchmarks.csproj -- --job Short --filter "*"
```

## Latest local run

Environment captured by BenchmarkDotNet:

- OS: Linux Ubuntu 24.04.4 LTS
- CPU: AMD EPYC 7763
- SDK: .NET 10.0.201
- Runtime: .NET 10.0.5

| Method | Job | Mean | Median | Allocated |
|---|---|---:|---:|---:|
| Generated registration startup | ShortRun | 2.007 μs | 2.001 μs | 5.68 KB |
| Reflection registration startup | ShortRun | 37.901 μs | 37.850 μs | 14.54 KB |

### Observations

- Generated startup registration is significantly faster than reflection-based scanning in the short-run sample.
- Generated startup registration allocates less memory.
- This benchmark is intentionally focused on startup registration/activation overhead.

## Profiling and optimization status

The benchmark work led to generator output optimization in `GenDISourceGenerator`:

- registration emission now uses lifetime-specific methods (`AddSingleton` / `AddScoped` / `AddTransient`) instead of explicit `ServiceDescriptor` construction per line.

This keeps generated registration code simpler and reduces startup registration overhead.
