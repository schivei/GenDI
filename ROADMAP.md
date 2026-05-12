# GenDI Roadmap

This document outlines the planned phases of development for GenDI.

---

## ✅ Phase 1 — Initial Structure and Attributes

**Goal**: Establish the project foundation and implement attribute-based service registration.

- [x] Create project structure (solution, projects, CI)
- [x] Implement `InjectableAttribute`
- [x] Source generator that detects `[Injectable]` classes
- [x] Generate `AddGenDIServices()` extension method
- [x] Unit tests for the generator

---

## ✅ Phase 2 — Attribute Model and Microsoft DI Integration

**Goal**: Expand attribute-based registration and fully integrate with `Microsoft.Extensions.DependencyInjection`.

- [x] Implement `ServiceInjectionAttribute`
- [x] Implement `GenDICoverationAttribute` for generated coverage control
- [x] Source generator support for inheritance/interface traversal with `ServiceInjectionAttribute`
- [x] Source generator support for additive `Injectable<TService>` registrations
- [x] Registration ordering support (`Group`, `Order`, service name)
- [x] Support for `Singleton`, `Scoped`, and `Transient` lifetimes
- [x] Integration tests with a real `IServiceCollection`

---

## ✅ Phase 3 — Advanced NativeAOT Support

**Goal**: Ensure full compatibility with NativeAOT publish and IL trimming.

- [x] Add `ILLink.xml` descriptors to preserve generated types
- [x] Validate trimming compatibility with `<PublishTrimmed>true</PublishTrimmed>`
- [x] Validate NativeAOT with `<PublishAot>true</PublishAot>`
- [x] Document NativeAOT usage in README

---

## ✅ Phase 4 — Benchmarks, Documentation Website, and CI Hardening

**Goal**: Improve developer experience and release readiness while preparing optimization baselines.

- [x] Create Docusaurus website with English-first detailed documentation
- [x] Align website visual theme and layout with the `net-mediate` documentation style
- [x] Add GitHub Pages deployment pipeline for the website
- [x] Add CI/CD and scheduled publish workflows prepared for Sonar/NuGet with bypass (`continue-on-error`)
- [x] Add `versions.props` and `pack.props` package/build metadata following the `net-mediate` pattern
- [x] Add BenchmarkDotNet project
- [x] Benchmark startup registration time vs. reflection-based DI (all four strategies)
- [x] Profile and optimize generated code
- [x] Publish benchmark results in repository

---

## ✅ Phase 5 — Official NuGet Publication

**Goal**: Release GenDI publicly on NuGet.org.

- [x] Set up NuGet package metadata baseline (versioning/pack props and workflow scaffolding)
- [x] Configure GitHub Actions baseline for package publishing workflows
- [x] Announce on GitHub Discussions and social channels

---

## 📋 Phase 6 — Developer Experience and Ecosystem Expansion

**Goal**: Make GenDI the go-to DI companion for every .NET project — not just AOT.

### 🔬 Source-generator quality

- [x] `GenDI.Analyzers` companion package: IDE warnings for misconfigured `[Injectable]` / `[Inject]` usage
- [x] Diagnostic for `[Inject]` on non-init property (CS error surfaced as IDE hint)
- [x] Diagnostic for `[Injectable]` on abstract type or interface
- [ ] Code-fix provider: convert constructor injection to GenDI property injection automatically
- [ ] Incremental generator optimization: reduce rebuild cost on partial changes

### 🗂️ Registration model

- [ ] `[InjectOptional]` — nullable/optional property injection (skips unregistered services gracefully)
- [ ] `[ConditionalInjectable(environmentName)]` — environment-conditional registration
- [ ] `[DecoratorFor<TService>]` — decorator pattern auto-wiring
- [ ] `ServiceInjectionAttribute` lifetime override as fallback (`Injectable > ServiceInjection > Transient`)
- [ ] Indirect injection (`[Inject]`) with implementation scanning and closed-generic-only support
- [ ] `[Inject]` lifetime override precedence (`Inject > Injectable > ServiceInjection > Transient`) with registration tie-break (`Scoped > Singleton > Transient`)
- [ ] Thread isolation registration policy configurable via `Injectable` / `ServiceInjection`
- [ ] Dependency scanning across referenced solution libraries for centralized registration
- [ ] `OptionConfigAttribute` to bind concrete option types into `IOptions<>` using required configuration key/path
- [ ] Factory registration: `[InjectableFactory]` on static factory methods
- [ ] Module-based grouping: `[InjectableModule]` on a partial class to namespace registrations

### 🌐 Platform and framework support

- [ ] ASP.NET Core Minimal API — zero-ceremony endpoint service injection example and docs
- [ ] Blazor WebAssembly — validate and document property injection in WASM components
- [ ] MAUI / mobile AOT — validate NativeAOT path on iOS and Android publish targets
- [ ] Worker Service / hosted service integration example
- [ ] F# attribute support exploration (`[<Injectable>]`)

### 🧪 Testing ergonomics

- [ ] `GenDI.Testing` companion package: `ServiceBuilder` helper for unit tests using property injection
- [ ] Integration with `Microsoft.Extensions.DependencyInjection.Abstractions` test helpers
- [ ] Example project: real-world xUnit test suite using GenDI services

### 🛠️ Tooling and IDE integration

- [ ] Visual Studio item-template: "GenDI Service" scaffold (interface + implementation + attribute)
- [ ] Rider live template for `[Injectable]` + `[Inject]` service
- [ ] `dotnet new` template: `gendi-service` project template

### 📡 Observability and runtime insights

- [ ] `[ObservableService]` — generate OpenTelemetry activity spans around service method calls
- [ ] Startup registration summary log: emit registered services list at `Debug` level
- [ ] Generated dependency graph export (DOT format) for architecture review

### 🌍 Community and ecosystem

- [ ] GitHub Discussions Q&A category
- [ ] `CHANGELOG.md` keeping a public history of all releases
- [ ] Localized documentation (Portuguese, Spanish, German)
- [ ] Sample repository: full ASP.NET Core + GenDI real-world project
- [ ] Benchmark suite expansion: multi-assembly scenarios, Blazor WASM startup

---

See [CONTRIBUTING.md](CONTRIBUTING.md) to help drive any of these forward.
