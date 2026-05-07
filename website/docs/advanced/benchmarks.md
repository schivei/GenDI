# Benchmarks

GenDI Phase 4 adds a dedicated BenchmarkDotNet project to validate startup registration performance.

## Scope

The benchmark compares:

- **Generated registration** via `AddGenDIServices()`
- **Reflection registration** via runtime assembly scanning

The goal is to prove the startup benefit of source-generated registration over reflection-heavy alternatives.

## Benchmark project

- `tests/GenDI.Benchmarks`
- `StartupRegistrationBenchmarks`

Run locally:

```bash
dotnet run -c Release --project tests/GenDI.Benchmarks/GenDI.Benchmarks.csproj -- --job Short --filter "*"
```

## Latest result snapshot

| Method | Mean | Allocated |
|---|---:|---:|
| Generated registration startup | 2.007 μs | 5.68 KB |
| Reflection registration startup | 37.901 μs | 14.54 KB |

Generated registration is faster and allocates less memory in this scenario.

## Published benchmark reports

Full benchmark reports are published in the repository at:

- `docs/BENCHMARKS.md`
