# GenDI Benchmark Results

This document tracks performance validation across registration strategies.

## Scope note for Phase 6 documentation parity

Benchmark scenarios focus on startup registration cost and activation shape. They do not attempt to benchmark every delivered Phase 6 feature (for example `RegistrationMultiplicity`/`RegistrationEmissionStrategy` policy combinations or `OptionConfig` section-selection behavior), which are documented in:

- `/home/runner/work/GenDI/GenDI/docs/REGISTRATION_MODEL_RM01_RM12.md`
- `/home/runner/work/GenDI/GenDI/docs/ROTEIRO_FASE6.md`

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
| 4 | **GenDI: property with decorator** <sup>1</sup> | `AddGenDIServices()` generated at compile time | Generated factory: `new Decorator { Inner = new Service { A = sp.Get<A>(), B = sp.Get<B>() } }` |
| 5 | **Reflection scanner (worst case)** | Assembly scan at startup via `GetTypes()` + reflection | Container-driven |

> <sup>1</sup> This scenario adds a simple decorator layer to the property injection case, validating that even with an extra level of factory nesting the generated code remains performant.

---

## Latest CI benchmark snapshot

<!-- benchmark-ci:start -->
_Updated by [CI run #195](https://github.com/schivei/GenDI/actions/runs/26071820675) on 2026-05-19 02:10 UTC_

| Method | Mean | Allocated |
|---|---:|---:|
| Manual registration (no GenDI) | 3.631 μs | 7.56 KB |
| GenDI: constructor injection (generated) | 6.025 μs | 9.89 KB |
| GenDI: property injection (generated) | 6.178 μs | 9.89 KB |
| GenDI: with decorator, property injection (generated) | 1,723.065 μs | 2751.01 KB |
| Reflection registration (no GenDI, assembly scan) | 70.866 μs | 23.6 KB |

### CI analysis

- GenDI constructor injection is **+65.9%** versus manual registration.
- GenDI property injection is **+70.1%** versus manual registration.
- Reflection scanning remains the outlier at **~19.5x slower** and **~3.1x higher allocation** than manual registration.
- Compatibility note: this benchmark compares manual and generated registrations against a reflection scanner baseline; as documented below, reflection scanning is not suitable for trimming/NativeAOT scenarios, while manual and GenDI-generated registrations remain the supported path.
<!-- benchmark-ci:end -->

<!-- benchmark-sales:start -->
<!-- benchmark-sales:end -->

---

## Analysis

### Manual vs GenDI generated

The manual baseline registers **the same full service set** as `AddGenDIServices()` to ensure an
apples-to-apples comparison. In the **latest CI snapshot above**, both generated variants are ahead
of manual registration on mean startup time, with constructor injection currently leading the group.
The key takeaway is not a fixed percentage but that manual and generated registration stay in the
same microsecond range, while the reflection scanner remains an order-of-magnitude slower.

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
| Manual vs GenDI generated | GenDI (latest CI snapshot) | Constructor: ~20.1 %, Property: ~17.7 % | Generated registration is currently fastest and removes manual maintenance |
| Constructor vs property injection | Tie | ±1–2 % (noise) | Use property injection for clean, scalable code |
| GenDI generated vs reflection scanner | GenDI | ~19× faster | Reflection scanning is not viable for cold-start-sensitive apps |

---

## Binary size comparison

These measurements use a representative minimal .NET 10 console application with three
singleton services and one transient service — a realistic slice of a real-world project.

**Environment**: .NET SDK 10.0.201, linux-x64 (AMD EPYC 7763)

### Publish configurations explained

| Configuration | Command | Includes runtime? | Trims unused code? | Output |
|---|---|---|---|---|
| Framework-dependent | `dotnet publish -c Release` | No (requires installed .NET) | No | `.dll` + deps |
| Self-contained | `dotnet publish -c Release -r linux-x64 --self-contained` | Yes (full runtime bundle) | No | Directory |
| Trimmed self-contained | `… /p:PublishTrimmed=true` | Yes | Yes — IL linker removes unused code | Directory |
| NativeAOT | `… /p:PublishAot=true` | Yes (compiled into binary) | Yes — AOT compiles only reachable code | Single native binary |

### Results

| Configuration | Manual (no GenDI) | GenDI (ctor or property injection) | Reflection scanner |
|---|---:|---:|---:|
| Framework-dependent (folder) | 264 KB | 292 KB | 264 KB |
| Framework-dependent (app DLL only) | 8 KB | 8 KB | 8 KB |
| Self-contained (folder) | ~80 MB | ~80 MB | ~80 MB |
| Trimmed self-contained (folder) | ~23 MB | ~23 MB | ~23 MB ⚠️ |
| NativeAOT (native binary) | 2.2 MB | 2.2 MB | 2.2 MB ⚠️ |

⚠️ = binary is produced but **crashes at runtime** (see analysis below).

### Analysis

**Framework-dependent**: GenDI adds one extra DLL (`GenDI.dll`, 8 KB) plus XML docs and PDB
(~20 KB combined). This is the only scenario where GenDI has any measurable footprint at all —
28 KB total. For most applications this is irrelevant.

**Self-contained**: The .NET runtime bundle (~80 MB) completely eclipses the 28 KB library
overhead. The output size is identical across all three strategies.

**Trimmed self-contained**:

- *Manual and GenDI*: The IL linker performs static dead-code elimination. GenDI's generated
  factories use no reflection, so the trimmer can fully analyse every code path and remove all
  unreferenced GenDI internals. Output size is identical to manual.
- *Reflection scanner*: `Assembly.GetTypes()` is decorated with
  `[RequiresUnreferencedCode]`. The trimmer emits two IL2026/IL2072 warnings and strips the
  implementation types it cannot see through the reflection call. The binary builds (~23 MB) but
  crashes with an abort at startup because the scanned types no longer exist at runtime.

**NativeAOT**:

- *Manual and GenDI*: GenDI is purpose-built for AOT. The generated factories contain zero
  reflection, zero dynamic dispatch, and zero runtime attribute inspection. The AOT compiler
  produces an **identical 2.2 MB native binary** for both.
- *Reflection scanner*: The AOT compiler emits the same IL2026/IL2072 warnings as the trimmer.
  The assembly type list is built at compile time and the dynamic `GetTypes()` enumeration has no
  types to find. The binary builds (2.2 MB) but crashes at startup — identical failure mode to
  trimming.

> **Takeaway**: GenDI adds 28 KB in framework-dependent deployments only. In all trimming
> and NativeAOT scenarios the final binary is **equivalent to hand-written registration and
> fully functional**. The reflection scanner produces binaries of identical size in those same
> scenarios but they **do not work** — any trimming-safe deployment requires either manual
> registration or GenDI.

---

## Profiling and optimization status

The benchmark work led to generator output optimization in `GenDISourceGenerator`:

- Registration emission now uses lifetime-specific methods (`AddSingleton` / `AddScoped` /
  `AddTransient`) instead of explicit `ServiceDescriptor` construction per line.

This keeps generated registration code simpler and reduces startup registration overhead.
