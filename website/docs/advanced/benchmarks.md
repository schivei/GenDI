# 📊 Benchmarks

GenDI includes a dedicated BenchmarkDotNet project to validate startup registration performance
across four distinct strategies, giving developers the data to make an informed choice.

## 🎯 Scenarios

| # | Description | How registration happens | How activation happens |
|---|---|---|---|
| 1 | **Manual (no GenDI)** | ✍️ Hand-written `AddSingleton<>` / `AddTransient<>` | Container expression-tree compilation (one-time reflection) |
| 2 | **GenDI — constructor injection** | ⚡ `AddGenDIServices()` (compile-time generated) | Generated factory: `new Service(sp.Get<A>(), sp.Get<B>())` |
| 3 | **GenDI — property injection** | ⚡ `AddGenDIServices()` (compile-time generated) | Generated factory: `new Service { A = sp.Get<A>(), B = sp.Get<B>() }` |
| 4 | **Reflection scanner (worst case)** | 🐢 `Assembly.GetTypes()` scan at startup | Container expression-tree compilation |

## 🧪 Benchmark project

- 📁 `tests/GenDI.Benchmarks`
- 🔍 `StartupRegistrationBenchmarks`

Run locally:

```bash
dotnet run -c Release --project tests/GenDI.Benchmarks/GenDI.Benchmarks.csproj -- --job Short --filter "*"
```

## ⚡ Latest result snapshot

<!-- benchmark-ci:start -->
_Updated by CI run #114 on 2026-05-17 04:52 UTC_

| Method | Mean | Allocated |
|---|---:|---:|
| Manual registration (no GenDI) | 2.702 μs | 5.97 KB |
| GenDI: constructor injection (generated) | 2.114 μs | 6.1 KB |
| GenDI: property injection (generated) | 2.234 μs | 6.1 KB |
| Reflection registration (no GenDI, assembly scan) | 48.497 μs | 18.24 KB |

### CI analysis

- GenDI constructor injection is **-21.8%** versus manual registration.
- GenDI property injection is **-17.3%** versus manual registration.
- Reflection scanning remains the outlier at **~17.9x slower** and **~3.1x higher allocation** than manual registration.
- Compatibility note: this benchmark compares manual and generated registrations against a reflection scanner baseline; as documented below, reflection scanning is not suitable for trimming/NativeAOT scenarios, while manual and GenDI-generated registrations remain the supported path.
<!-- benchmark-ci:end -->

## 🔍 What the numbers mean

### ✍️ Manual vs ⚡ GenDI generated

The manual baseline registers **the same full service set** as `AddGenDIServices()` for an
apples-to-apples comparison. Manual registration is marginally faster (~8 %) because it inlines
the registration calls directly, while GenDI bundles them inside a generated extension method.
This is a **constant, one-time startup cost** — it has no effect on per-request resolution speed.

The ergonomic price of "manual" is every new service needing its own `AddScoped<>()` call in a
startup file. GenDI eliminates that maintenance entirely.

### 🏆 Constructor injection vs property injection — it's a tie

The performance difference between GenDI constructor injection and GenDI property injection is
**±1–2 % — within measurement noise**. Both generate an explicit compiled factory lambda; the
JIT produces nearly identical machine code.

> 💡 **Choose property injection for cleaner code. You pay no measurable performance price.**

### 🚨 Reflection scanner — the real cost to avoid

Assembly scanning at startup is **~19× slower** and allocates **~2.5× more memory** than any
GenDI-generated strategy. GenDI moves all of that scanning to compile time — the runtime never
touches a `GetTypes()` call.

## 📋 Summary

| Comparison | 🏆 Winner | Margin | Takeaway |
|---|---|---|---|
| ✍️ Manual vs ⚡ GenDI generated | Manual (barely) | ~8 % | Negligible; GenDI saves hours of maintenance |
| ⚡ Constructor vs 🏆 property injection | Tie | ±1–2 % (noise) | **Use property injection — zero cost, big ergonomic win** |
| ⚡ GenDI generated vs 🐢 reflection scanner | GenDI | ~19× faster | Reflection scanning is not viable for cold-start-sensitive apps |

## 📦 Binary size comparison

These measurements use a representative minimal .NET 10 console application with three
singleton services and one transient service.

**🖥️ Environment**: .NET SDK 10.0.201, linux-x64

### Results

| Configuration | ✍️ Manual (no GenDI) | ⚡ GenDI (ctor or property) | 🐢 Reflection scanner |
|---|---:|---:|---:|
| Framework-dependent (folder) | 264 KB | 292 KB | 264 KB |
| Framework-dependent (app DLL) | 8 KB | 8 KB | 8 KB |
| Self-contained (folder) | ~80 MB | ~80 MB | ~80 MB |
| Trimmed self-contained (folder) | ~23 MB | ~23 MB | ~23 MB ⚠️ |
| NativeAOT (native binary) | 2.2 MB | 2.2 MB | 2.2 MB ⚠️ |

⚠️ = binary is produced but **crashes at runtime**.

### 🔍 What this means

- ✅ **Framework-dependent**: GenDI adds 28 KB (the library DLL + PDB + XML docs). Irrelevant for
  any real deployment. Reflection scanner has no overhead here.
- ✅ **Self-contained**: The .NET runtime bundle (~80 MB) eclipses everything. All three strategies
  produce identical output sizes.
- ✅ **Trimmed**: The IL linker statically analyses the generated factories (no reflection → full
  visibility) and removes all unused GenDI internals. Final size is identical to manual.  
  ❌ The reflection scanner triggers IL2026 / IL2072 trimmer warnings — the implementation types
  get stripped and the binary crashes at startup.
- ✅ **NativeAOT**: GenDI generates zero-reflection factory code. The AOT compiler produces an
  **identical 2.2 MB native binary** to hand-written registration.  
  ❌ The reflection scanner generates the same compiler warnings and the native binary crashes at
  startup — `Assembly.GetTypes()` is incompatible with AOT.

> 🎯 GenDI adds 28 KB in framework-dependent mode only. Under trimming or NativeAOT the
> final binary is equivalent to writing every `Add*<>()` call by hand — **and actually works**.
> The reflection scanner produces equally-sized binaries but they **crash at runtime** in both
> trimming and AOT scenarios.

To reproduce locally:

```bash
# Framework-dependent
dotnet publish MyApp.csproj -c Release -o out-fd

# Self-contained
dotnet publish MyApp.csproj -c Release -r linux-x64 --self-contained -o out-sc

# Trimmed
dotnet publish MyApp.csproj -c Release -r linux-x64 --self-contained /p:PublishTrimmed=true -o out-trimmed

# NativeAOT
dotnet publish MyApp.csproj -c Release -r linux-x64 /p:PublishAot=true -o out-aot
```

## 📄 Published benchmark reports

Full benchmark reports are published in the repository at:

- 📄 `docs/BENCHMARKS.md`
