# Benchmarks

GenDI includes a dedicated BenchmarkDotNet project to validate startup registration performance
across four distinct strategies, giving developers the data to make an informed choice.

## Scenarios

| # | Description | How registration happens | How activation happens |
|---|---|---|---|
| 1 | **Manual (no GenDI)** | Hand-written `AddSingleton<>` / `AddTransient<>` | Container expression-tree compilation (one-time reflection) |
| 2 | **GenDI — constructor injection** | `AddGenDIServices()` (compile-time generated) | Generated factory: `new Service(sp.Get<A>(), sp.Get<B>())` |
| 3 | **GenDI — property injection** | `AddGenDIServices()` (compile-time generated) | Generated factory: `new Service { A = sp.Get<A>(), B = sp.Get<B>() }` |
| 4 | **Reflection scanner (worst case)** | `Assembly.GetTypes()` scan at startup | Container expression-tree compilation |

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
| Manual registration (no GenDI) | 1.842 μs | 5.21 KB |
| GenDI: constructor injection (generated) | 2.007 μs | 5.68 KB |
| GenDI: property injection (generated) | 2.031 μs | 5.71 KB |
| Reflection registration (no GenDI, assembly scan) | 37.901 μs | 14.54 KB |

## What the numbers mean

### Manual vs GenDI generated

Manual registration is marginally faster (~8 %) than the generated path because it skips the
generated extension method and calls the Microsoft DI API directly. This is a **constant,
one-time startup cost** — it has no effect on per-request resolution speed.

The ergonomic price of "manual" is every new service needing its own `AddScoped<>()` call in a
startup file. GenDI eliminates that maintenance entirely.

### Constructor injection vs property injection — it's a tie

The performance difference between GenDI constructor injection and GenDI property injection is
**±1–2 % — within measurement noise**. Both generate an explicit compiled factory lambda; the
JIT produces nearly identical machine code.

> **Choose property injection for cleaner code. You pay no measurable performance price.**

### Reflection scanner — the real cost to avoid

Assembly scanning at startup is ~19× slower and allocates ~2.5× more memory than any
GenDI-generated strategy. GenDI moves all of that scanning to compile time — the runtime never
touches a `GetTypes()` call.

## Summary

| Comparison | Winner | Margin | Takeaway |
|---|---|---|---|
| Manual vs GenDI generated | Manual (barely) | ~8 % | Negligible; GenDI saves hours of maintenance |
| Constructor vs property injection | Tie | ±1–2 % (noise) | **Use property injection — zero cost, big ergonomic win** |
| GenDI generated vs reflection scanner | GenDI | ~19× faster | Reflection scanning is not viable for cold-start-sensitive apps |

## Published benchmark reports

Full benchmark reports are published in the repository at:

- `docs/BENCHMARKS.md`
