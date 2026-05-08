# GenDI Benchmark Results

This document tracks performance validation across registration strategies.

## Benchmark project

- Project: `tests/GenDI.Benchmarks`
- Tooling: BenchmarkDotNet (`0.15.6`)
- Focus: compare **all four startup registration strategies** to give developers real data.

### Run command

```bash
dotnet run -c Release --project tests/GenDI.Benchmarks/GenDI.Benchmarks.csproj -- --job Short --filter "*"
```

---

## Scenarios

| # | Description | Registration | Activation |
|---|---|---|---|
| 1 | **Manual (no GenDI)** | Hand-written `AddSingleton<>` / `AddTransient<>` | Container-driven (expression trees, one-time reflection per type) |
| 2 | **GenDI — constructor injection** | `AddGenDIServices()` generated at compile time | Generated factory: `new Service(sp.Get<A>(), sp.Get<B>())` |
| 3 | **GenDI — property injection** | `AddGenDIServices()` generated at compile time | Generated factory: `new Service { A = sp.Get<A>(), B = sp.Get<B>() }` |
| 4 | **Reflection scanner (worst case)** | Assembly scan at startup via `GetTypes()` + reflection | Container-driven |

---

## Latest local run

Environment captured by BenchmarkDotNet:

- OS: Linux Ubuntu 24.04.4 LTS
- CPU: AMD EPYC 7763
- SDK: .NET 10.0.201
- Runtime: .NET 10.0.5

| Method | Job | Mean | Median | Allocated |
|---|---|---:|---:|---:|
| Manual registration (no GenDI) | ShortRun | 1.842 μs | 1.838 μs | 5.21 KB |
| GenDI: constructor injection (generated) | ShortRun | 2.007 μs | 2.001 μs | 5.68 KB |
| GenDI: property injection (generated) | ShortRun | 2.031 μs | 2.027 μs | 5.71 KB |
| Reflection registration (no GenDI, assembly scan) | ShortRun | 37.901 μs | 37.850 μs | 14.54 KB |

---

## Analysis

### Manual vs GenDI generated

Manual hand-written registration is marginally faster (~8 %) than the generated path because
it calls the Microsoft DI registration API directly without going through a generated extension
method. This small overhead is a constant, one-time startup cost and has **no effect** on
per-request service resolution speed.

**Trade-off**: manual registration requires writing, maintaining, and reviewing every `Add*<>()`
call by hand. GenDI eliminates that entirely — every new service registers itself at compile time.

### Constructor injection vs property injection (GenDI)

The difference between constructor injection and property injection in GenDI-generated code is
**within measurement noise (≈1–2 %)** — effectively zero.

This is expected: both styles emit an explicit compiled factory lambda.

- Constructor: `new Service(sp.GetRequiredService<A>(), sp.GetRequiredService<B>())`
- Property:    `new Service { A = sp.GetRequiredService<A>(), B = sp.GetRequiredService<B>() }`

The C# compiler and JIT produce nearly identical machine code for both. **Choose property
injection for ergonomics and readability — you pay no measurable performance price.**

### Reflection scanner (worst case)

Assembly scanning at startup is **~19× slower** and allocates **~2.5× more memory** than
GenDI-generated registration. This is the cost of `Assembly.GetTypes()`, custom attribute
inspection, and dynamic descriptor construction — all of which GenDI moves to compile time.

---

## Summary table

| Comparison | Winner | Margin | Takeaway |
|---|---|---|---|
| Manual vs GenDI generated | Manual (barely) | ~8 % | Negligible; GenDI saves hours of maintenance |
| Constructor vs property injection | Tie | ±1–2 % (noise) | Use property injection for clean, scalable code |
| GenDI generated vs reflection scanner | GenDI | ~19× faster | Reflection scanning is not viable for cold-start-sensitive apps |

---

## Profiling and optimization status

The benchmark work led to generator output optimization in `GenDISourceGenerator`:

- Registration emission now uses lifetime-specific methods (`AddSingleton` / `AddScoped` /
  `AddTransient`) instead of explicit `ServiceDescriptor` construction per line.

This keeps generated registration code simpler and reduces startup registration overhead.
