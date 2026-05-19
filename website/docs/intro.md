---
sidebar_position: 1
---

# 🧩 Introduction to GenDI

[![CI/CD Pipeline](https://github.com/schivei/GenDI/actions/workflows/ci-cd.yml/badge.svg)](https://github.com/schivei/GenDI/actions/workflows/ci-cd.yml)
[![Deploy Documentation](https://github.com/schivei/GenDI/actions/workflows/deploy-docs.yml/badge.svg)](https://github.com/schivei/GenDI/actions/workflows/deploy-docs.yml)
[![NuGet GenDI](https://img.shields.io/nuget/v/GenDI.svg)](https://www.nuget.org/packages/GenDI)
[![NuGet GenDI.SourceGenerator](https://img.shields.io/nuget/v/GenDI.SourceGenerator.svg)](https://www.nuget.org/packages/GenDI.SourceGenerator)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://github.com/schivei/GenDI/blob/main/LICENSE.md)

GenDI is an attribute-first dependency injection source generator for .NET. It generates DI registrations and activation code at **compile time** — no reflection, no runtime scanning, no boilerplate.

## 😤 Stop writing constructor boilerplate

Every .NET developer knows the pain: a service gains one more dependency, so you update the constructor signature, add another private field, and repeat the same assignment yet again. As a codebase grows, constructors become maintenance burdens rather than meaningful code.

GenDI eliminates this ceremony entirely with **property injection**:

```csharp
// ❌ Traditional constructor injection — repetitive, noisy, hard to extend
public class ReportService
{
    private readonly IReportRepository _repo;
    private readonly IEmailService _email;
    private readonly IStorageService _storage;
    private readonly ILogger<ReportService> _logger;

    public ReportService(
        IReportRepository repo,
        IEmailService email,
        IStorageService storage,
        ILogger<ReportService> logger)
    {
        _repo = repo;
        _email = email;
        _storage = storage;
        _logger = logger;
    }
}
```

```csharp
// ✅ GenDI property injection — clean, declarative, self-documenting
[Injectable<IReportService>(ServiceLifetime.Scoped)]
public class ReportService : IReportService
{
    [Inject] public required IReportRepository Repo { get; init; }
    [Inject] public required IEmailService Email { get; init; }
    [Inject] public required IStorageService Storage { get; init; }
    [Inject] public required ILogger<ReportService> Logger { get; init; }
}
```

The `required` keyword guarantees that every dependency is provided — the compiler enforces it. GenDI generates the wiring code so you never touch it.

## 🏆 Why property injection wins

| | Constructor injection | GenDI property injection |
|---|---|---|
| ➕ Adding a dependency | Edit ctor signature + field + assignment | Add one `[Inject]` property |
| 👀 Reading the class | Ctor signature + private fields | Properties listed at a glance |
| 🧪 Unit testing | Build a mock for every ctor param | Assign only the props you need |
| 🔀 Refactoring | Risk of parameter order mistakes | Properties are named — no position bugs |
| ⚙️ Generated code | None | Explicit, readable, debuggable |

## ✨ Key features and practical value

- 🎯 **Property injection as first-class citizen**: `[Inject]` on `required` init-only properties — dependencies read like documentation, not plumbing.
- 🚫 **Zero boilerplate registration**: one `[Injectable]` attribute replaces manual `AddScoped<>()` calls in startup files.
- 📦 **Package bundle ready**: `GenDI.SourceGenerator` ships with `GenDI.Analyzers` and transitive `Using Include="GenDI"` for smoother consumer setup.
- 🔒 **Compile-time safety**: the C# compiler enforces that every `required` `[Inject]` property is assigned in the generated initializer — you cannot accidentally omit a dependency. Note: unregistered services still surface as runtime container exceptions, just like standard DI.
- 📖 **Readable generated flow**: activation uses explicit `new` + `GetRequiredService<T>()`, easy to inspect and debug.
- 📐 **Predictable behavior**: deterministic ordering with `Group` and `Order` avoids ambiguous pipeline composition.
- 🔑 **Modern DI scenarios**: keyed registrations and keyed resolution through both Microsoft DI and GenDI attributes.
- ⚡ **No startup overhead**: compile-time generation eliminates reflection-based scanning costs.
- 🚀 **Future-proof deployment**: NativeAOT and trimming support when you need it — no friction for traditional deployments.

## 💡 Why GenDI exists

Traditional runtime scanning is practical, but adds startup cost and can break with aggressive trimming or NativeAOT. GenDI shifts all of this to compile-time:

- Registration mapping is generated from attributes.
- Property and constructor injection are generated as strongly typed C# code.
- Microsoft DI remains the runtime container — GenDI is purely additive.

## 📋 Feature summary

- `[Injectable]` and `[Injectable<TService>]` to mark concrete services
- `[ServiceInjection]` to mark interfaces/abstract contracts
- `[Inject]` for init-only property injection (`get; init;`) — the idiomatic GenDI way
- `RegistrationMultiplicity` + `RegistrationEmissionStrategy` to control `Single`/`Multiple` and `Add`/`TryAdd` generation
- `[OptionConfig]` with optional key fallback to type name for `IOptions<T>` binding
- `[assembly: GenDICoveration(...)]` to control generated extension coverage behavior
- Ordering by `Group`, then `Order`, then service type name (ordinal)

## ⚙️ Typical generation output

GenDI generates an extension method in the consumer assembly namespace:

```csharp
using <AssemblyName>.DependencyInjection;

....

builder.Host.UseGenDI();
builder.Services.AddGenDIServices();
```

Each registration uses generated `new` expressions and `GetRequiredService<T>()`, keeping activation explicit and analyzer-friendly.

## 🗺️ Documentation map

- 📦 **Getting Started**: installation and first setup
- 📚 **Core Concepts**: attributes, contracts, registration strategy
- 🔬 **Advanced**: NativeAOT/trimming validation, benchmarks and test strategy
- 🧭 **Analyzer diagnostics**: reference list for `GENDI001+` and IDE help links
- 🌍 **Community**: contribution, roadmap and sponsorship

## 📌 Phase 6 status baseline

Phase 6 track status is synchronized with:

- `/home/runner/work/GenDI/GenDI/docs/ROTEIRO_FASE6.md` (repository canonical matrix)
