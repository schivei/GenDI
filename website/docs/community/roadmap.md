# 🗺️ Roadmap

## 📊 Current status

| Phase | Description | Status |
|---|---|---|
| 1 | 🏗️ Foundation — `InjectableAttribute`, source generator, unit tests | ✅ Done |
| 2 | 🔌 Attribute model, Microsoft DI integration, ordering, lifetimes | ✅ Done |
| 3 | 🚀 NativeAOT / trimming validation | ✅ Done |
| 4 | 📈 Benchmarks, Docusaurus website, CI hardening | ✅ Done |
| 5 | 📦 NuGet publication and announcement | ✅ Done |
| 6 | 🌟 Developer experience, ecosystem expansion | 📋 Planned |

For the full phase-by-phase checklist, see [`ROADMAP.md`](https://github.com/schivei/GenDI/blob/main/ROADMAP.md) in the repository.

---

## 🌟 Phase 6 highlights — what's coming

### 🔬 Source-generator quality
- `GenDI.Analyzers` companion package — IDE warnings for misconfigured `[Injectable]` / `[Inject]` usage
- Code-fix provider: convert constructor injection to GenDI property injection automatically
- Incremental generator optimization to reduce rebuild cost on partial changes

### 🗂️ Registration model
- ✅ `[InjectOptional]` — optional property injection (skips unregistered services gracefully)
- ✅ `[ConditionalInjectable(environmentName)]` — environment-conditional registration
- ✅ `[DecoratorFor<TService>]` — decorator pattern auto-wiring
- ✅ Indirect injection via `[Inject]` with closed-generic-only implementation scanning
- ✅ `[Inject]` lifetime override precedence (`Inject > Injectable > ServiceInjection > Transient`) and tie-break
- ✅ Thread-isolation registration policy configurable via `Injectable` / `ServiceInjection`
- ✅ Dependency scanning across referenced libraries for centralized registration
- ✅ Closed-generic indirect inference for inferable concrete implementations
- ✅ `OptionConfigAttribute` for `IOptions<>` registration with required key/path
- ✅ Static factory registration with `[InjectableFactory]`
- ✅ Module-based grouping with `[InjectableModule]`

### 🌐 Platform support
- Blazor WebAssembly validated property injection
- MAUI / mobile AOT on iOS and Android
- Worker Service and hosted service integration examples
- F# attribute support exploration

### 🧪 Testing
- `GenDI.Testing` companion package with `ServiceBuilder` helper for unit tests using property injection

### 🛠️ Tooling and IDE
- Visual Studio item-template: "GenDI Service" scaffold
- `dotnet new` template: `gendi-service`
- Rider live template

### 📡 Observability
- `[ObservableService]` — auto-generated OpenTelemetry spans around service method calls
- Startup registration summary log at `Debug` level

---

Want to help make any of these real? See [Contributing](./contributing) to get started.
