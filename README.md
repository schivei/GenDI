# GenDI

> **Generator-based Dependency Injection for .NET — elegant, fast, AOT-ready**

[![CI/CD Pipeline](https://github.com/schivei/GenDI/actions/workflows/ci-cd.yml/badge.svg)](https://github.com/schivei/GenDI/actions/workflows/ci-cd.yml)
[![Deploy Documentation](https://github.com/schivei/GenDI/actions/workflows/deploy-docs.yml/badge.svg)](https://github.com/schivei/GenDI/actions/workflows/deploy-docs.yml)
[![NuGet GenDI](https://img.shields.io/nuget/v/GenDI.svg?style=flat&label=GenDI&logo=nuget)](https://www.nuget.org/packages/GenDI)
[![NuGet GenDI.SourceGenerator](https://img.shields.io/nuget/v/GenDI.SourceGenerator.svg?style=flat&label=GenDI.SourceGenerator&logo=nuget)](https://www.nuget.org/packages/GenDI.SourceGenerator)
[![NuGet GenDI.Testing](https://img.shields.io/nuget/v/GenDI.Testing.svg?style=flat&label=GenDI.Testing&logo=nuget)](https://www.nuget.org/packages/GenDI.Testing)
[![NuGet GenDI.Analyzers](https://img.shields.io/nuget/v/GenDI.Analyzers.svg?style=flat&label=GenDI.Analyzers&logo=nuget)](https://www.nuget.org/packages/GenDI.Analyzers)

[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=schivei_GenDI&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=schivei_GenDI)
[![Bugs](https://sonarcloud.io/api/project_badges/measure?project=schivei_GenDI&metric=bugs)](https://sonarcloud.io/summary/new_code?id=schivei_GenDI)
[![Code Smells](https://sonarcloud.io/api/project_badges/measure?project=schivei_GenDI&metric=code_smells)](https://sonarcloud.io/summary/new_code?id=schivei_GenDI)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=schivei_GenDI&metric=coverage)](https://sonarcloud.io/summary/new_code?id=schivei_GenDI)
[![Duplicated Lines (%)](https://sonarcloud.io/api/project_badges/measure?project=schivei_GenDI&metric=duplicated_lines_density)](https://sonarcloud.io/summary/new_code?id=schivei_GenDI)
[![Lines of Code](https://sonarcloud.io/api/project_badges/measure?project=schivei_GenDI&metric=ncloc)](https://sonarcloud.io/summary/new_code?id=schivei_GenDI)
[![Reliability Rating](https://sonarcloud.io/api/project_badges/measure?project=schivei_GenDI&metric=reliability_rating)](https://sonarcloud.io/summary/new_code?id=schivei_GenDI)
[![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=schivei_GenDI&metric=security_rating)](https://sonarcloud.io/summary/new_code?id=schivei_GenDI)
[![Technical Debt](https://sonarcloud.io/api/project_badges/measure?project=schivei_GenDI&metric=sqale_index)](https://sonarcloud.io/summary/new_code?id=schivei_GenDI)
[![Maintainability Rating](https://sonarcloud.io/api/project_badges/measure?project=schivei_GenDI&metric=sqale_rating)](https://sonarcloud.io/summary/new_code?id=schivei_GenDI)
[![Vulnerabilities](https://sonarcloud.io/api/project_badges/measure?project=schivei_GenDI&metric=vulnerabilities)](https://sonarcloud.io/summary/new_code?id=schivei_GenDI)


[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://github.com/schivei/GenDI/blob/main/LICENSE)
[![Documentation](https://img.shields.io/badge/Documentation-Website-blue)](https://elton.schivei.nom.br/GenDI)

GenDI is a dependency injection library built on top of C# *source generators*, providing full compatibility with NativeAOT and trimming. It works as an additional module to `Microsoft.Extensions.DependencyInjection`, allowing you to register services automatically at compile time — no reflection required.

<!-- benchmark-sales:start -->
## Why teams adopt GenDI

> Latest CI benchmarks show **GenDI constructor injection is 21.0% faster than manual registration**.

- **Move faster**: replace repetitive `AddScoped<>` / `AddSingleton<>` wiring with compile-time generation.
- **Start faster**: keep registrations out of reflection scanners and on the fast path for startup.
- **Deploy safely**: stay ready for trimming and NativeAOT without giving up readable DI code.
- **Scale cleanly**: property injection and generated factories keep large services maintainable.

[See the latest benchmark details](./docs/BENCHMARKS.md)
<!-- benchmark-sales:end -->

## Say goodbye to constructor bloat

Real-world services accumulate dependencies. With traditional constructor injection this tends to look like this:

```csharp
// ❌ The "constructor tax" — grows every time a new dependency is added
public class OrderProcessor
{
    private readonly IOrderRepository _orderRepository;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IEmailService _emailService;
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<OrderProcessor> _logger;

    public OrderProcessor(
        IOrderRepository orderRepository,
        IPaymentGateway paymentGateway,
        IEmailService emailService,
        IInventoryService inventoryService,
        ILogger<OrderProcessor> logger)
    {
        _orderRepository = orderRepository;
        _paymentGateway = paymentGateway;
        _emailService = emailService;
        _inventoryService = inventoryService;
        _logger = logger;
    }
}
```

With GenDI's **property injection**, the same class becomes clean and self-documenting:

```csharp
// ✅ Declare what you need — GenDI wires everything at compile time
[Injectable<IOrderProcessor>(ServiceLifetime.Scoped)]
public class OrderProcessor : IOrderProcessor
{
    [Inject] public required IOrderRepository OrderRepository { get; init; }
    [Inject] public required IPaymentGateway PaymentGateway { get; init; }
    [Inject] public required IEmailService EmailService { get; init; }
    [Inject] public required IInventoryService InventoryService { get; init; }
    [Inject] public required ILogger<OrderProcessor> Logger { get; init; }
}
```

No private fields. No constructor ceremony. No manual wiring. Just declare your dependencies and move on.

## Key features and developer benefits

- **Property injection as first-class citizen**: use `[Inject]` on `required` init-only properties — dependencies read like documentation, not plumbing.
- **Zero boilerplate registration**: a single `[Injectable]` attribute replaces `AddScoped<TImpl>()` calls scattered across startup files.
- **Readable generated flow**: activation is emitted as explicit `new` + `GetRequiredService<T>()`, making the wiring transparent and debuggable.
- **Compile-time safety**: the C# compiler enforces every `required` `[Inject]` property is assigned — you cannot accidentally skip a dependency. Container registration errors (unregistered services) remain runtime exceptions, just like standard DI.
- **Deterministic registration order**: `Group` + `Order` give you predictable, testable pipeline composition.
- **Attribute-first contract mapping**: combine `[Injectable]`, `[Injectable<TService>]`, and `[ServiceInjection]` with clear intent.
- **Keyed services support**: works with both native `[FromKeyedServices]` and GenDI `[Inject(Key = ...)]`.
- **Factory-first registration**: use `[InjectableFactory<TService>]` on static methods when construction should be centralized.
- **Module filtering**: group registrations with `[InjectableModule]` / `Module` and load only selected modules.
- **Registration strategy control**: `RegistrationMultiplicity` + `RegistrationEmissionStrategy` let you choose `Single`/`Multiple` and `Add`/`TryAdd` generation semantics.
- **Options mapping evolution**: `[OptionConfig]` supports optional key fallback (`type name`) plus optimized `AddOptions<T>().BindConfiguration(section)` registration.
- **Testing ergonomics**: `GenDI.Testing` includes a fluent `ServiceBuilder` for xUnit/unit-test composition.
- **Open-generic safety**: open-generic registrations are bypassed and reported as generator warnings (`GENDISG001`).
- **No runtime scanning cost**: compile-time generation eliminates startup overhead from reflection-based scanning.
- **AOT/trimming friendly by design**: safe path for teams that need NativeAOT, without forcing this concern for every project.

---

## Installation

```bash
dotnet add package GenDI
dotnet add package GenDI.SourceGenerator
dotnet add package GenDI.Testing
```

`GenDI.SourceGenerator` now bundles `GenDI.Analyzers` and ships `buildTransitive/GenDI.SourceGenerator.props` (`Using Include="GenDI"`).  
`GenDI` remains the runtime package (no buildTransitive content).  
When using `GenDI.SourceGenerator`, you normally **should not** install `GenDI.Analyzers` separately to avoid duplicate diagnostics/code-fix hints.

---

## Usage

### Using `InjectableAttribute`

```csharp
[ServiceInjection]
public interface IMyService
{
    void Execute();
}

[Injectable(ServiceLifetime.Singleton, Group = 10, Order = 1)]
public class MyService : IMyService
{
    public void Execute() => Console.WriteLine("Service injected!");
}
```

`ServiceInjectionAttribute` also supports an optional fallback lifetime:

```csharp
[ServiceInjection(ServiceLifetime.Scoped)]
public interface IScopedContract
{
}
```

Thread isolation fallback can also be configured at contract level:

```csharp
[ServiceInjection(ServiceLifetime.Scoped, ThreadIsolation = ThreadIsolationPolicy.Singleton)]
public interface IThreadIsolatedContract
{
}
```

Fallback precedence is: `Injectable > ServiceInjection > Transient`.

`InjectableAttribute` supports:

- `Lifetime` (constructor argument, default `Transient`)
- `Group` (optional, default `int.MaxValue`)
- `Order` (optional, default `int.MaxValue`)
- `ServiceType`:
  - `[Injectable]` -> `null` (no explicit contract)
  - `[Injectable<TService>]` -> `typeof(TService)` as explicit contract (additive with `[ServiceInjection]`)
- `Key` (optional, default `null`) for keyed service registration generation
- `ThreadIsolation` (optional) using `ThreadIsolationPolicy.{Singleton|Scoped|Transient}` for thread-local resolution cache
- `Module` (optional) to associate registration with a module group

Service registration emission order is:
1. `Group`
2. `Order`
3. Service type name (ordinal)

### Registering Services

```csharp
using YourProject.DependencyInjection; // generated by GenDI in the consumer project namespace

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGenDIServices();
var app = builder.Build();
app.Run();
```

### Property Injection with `[Inject]`

Declare dependencies as `required` init-only properties and mark them with `[Inject]`. GenDI generates the activation code — no constructor needed:

```csharp
[Injectable<IOrderProcessor>(ServiceLifetime.Scoped)]
public class OrderProcessor : IOrderProcessor
{
    [Inject] public required IOrderRepository Repository { get; init; }
    [Inject] public required IPaymentGateway Payment { get; init; }
    [Inject] public required ILogger<OrderProcessor> Logger { get; init; }

    public async Task ProcessAsync(Order order)
    {
        Logger.LogInformation("Processing order {Id}", order.Id);
        await Repository.SaveAsync(order);
        await Payment.ChargeAsync(order);
    }
}
```

`[Inject]` also supports optional `Key` for keyed dependency resolution:

```csharp
[Inject(Key = "primary")]
public required IMyService Service { get; init; }
```

`[Inject]` also supports lifetime override for indirect registration discovery:

```csharp
[Inject(ServiceLifetime.Scoped)]
public required IMyService Service { get; init; }
```

Precedence for indirect registration lifetime is:
`Inject > Injectable > ServiceInjection > Transient` (tie-break favors `Scoped > Singleton > Transient`).

For optional dependencies that should not throw when unregistered, use `[InjectOptional]`:

```csharp
[InjectOptional]
public required IMyService? OptionalService { get; init; }
```

For environment-conditional registration, combine `[Injectable]` with `[ConditionalInjectable]`:

```csharp
[Injectable<IMyService>(ServiceLifetime.Singleton)]
[ConditionalInjectable("Development")]
public sealed class DevOnlyService : IMyService { }
```

For decorators, mark the wrapper with `[DecoratorFor<TService>]` or let GenDI infer the
`[ServiceInjection]` contract with non-generic `[DecoratorFor(Order = ...)]`:

```csharp
[Injectable<IMyService>(ServiceLifetime.Singleton)]
public sealed class CoreService : IMyService
{
    public void Execute() { }
}

[DecoratorFor<IMyService>(Order = 0)]
public sealed class LoggingDecorator(IMyService inner) : IMyService
{
    public void Execute() => inner.Execute();
}

[DecoratorFor(Order = 1)]
public sealed class ValidationDecorator : IMyService
{
    [Inject]
    public required IMyService Inner { get; init; }

    public void Execute() => Inner.Execute();
}
```

Decorator pipelines are emitted statically in ascending `Order`; ties fall back to the decorator
type name using ordinal comparison. Decorators must expose a public constructor parameter or
`[Inject]` init-only property matching the decorated service contract.

For factory registration, annotate static factory methods:

```csharp
[InjectableModule("Billing")]
public static class BillingFactories
{
    [InjectableFactory<IMyService>(ServiceLifetime.Singleton)]
    public static IMyService Create() => new MyService();
}
```

> ⚠️ `[InjectableFactory<TService>]` supports only **closed-generic** types. Open-generic return types, parameters, generic factory methods, or generic containing types are ignored and emitted as warnings.

To bind options automatically:

```csharp
[OptionConfig("Features:MyOptions")]
public sealed class MyOptions
{
    public string? Name { get; init; }
}
```

Constructor injection is also supported and can use the native DI attribute:

```csharp
public MyConsumer([FromKeyedServices("primary")] IMyService service) { }
```

### Analyzer diagnostics (`GenDI.Analyzers`)

`GenDI.Analyzers` currently publishes:

- `GENDI001` — `[Inject]` requires `init`-only property
- `GENDI002` — `[Injectable]` requires concrete non-abstract class
- `GENDI003` — constructor injection can be converted to GenDI property injection (code-fix available)
- `GENDI004` — non-generic `[DecoratorFor]` must resolve exactly one closed `[ServiceInjection]` contract
- `GENDI005` — decorators must expose the decorated contract as a constructor parameter or `[Inject]` property

Official diagnostics list:

- [docs/ANALYZER_DIAGNOSTICS.md](docs/ANALYZER_DIAGNOSTICS.md)

### Service Contract Discovery

- GenDI discovers services from `[ServiceInjection]` in implemented interfaces and base types.
- `Injectable<TService>` is also added to the generated registration list when provided.
- If no `[ServiceInjection]` is found in the inheritance/implementation chain, the concrete class is registered as its own service.

### Generated Coverage Configuration

By default, generated extensions are included in coverage (no `[ExcludeFromCodeCoverage]`).
You can control this per assembly:

```csharp
[assembly: GenDI.GenDICoveration(false)] // add [ExcludeFromCodeCoverage] to generated extension
```

## NativeAOT and Trimming (Phase 3)

GenDI includes linker descriptors and validation projects for trimming and NativeAOT scenarios.

### Publish with trimming

```bash
dotnet publish tests/GenDI.Phase3.TrimValidation.App/GenDI.Phase3.TrimValidation.App.csproj -c Release
```

### Publish with NativeAOT

```bash
dotnet publish tests/GenDI.Phase3.NativeAotValidation.App/GenDI.Phase3.NativeAotValidation.App.csproj -c Release -r linux-x64
```

### ILLink descriptor sample

```xml
<linker>
  <assembly fullname="YourAssemblyName">
    <type fullname="YourAssemblyName.DependencyInjection.GenDIServiceCollectionExtensions" preserve="all" />
  </assembly>
</linker>
```

## Documentation Website (Phase 4)

GenDI now ships an English-first Docusaurus documentation website under `website/`, with a theme aligned to `net-mediate`.

### Local docs development

```bash
cd website
npm ci
npm run start
```

### Production docs build

```bash
cd website
npm run build
```

GitHub Pages deployment is handled by `.github/workflows/deploy-docs.yml`.

## Benchmarks (Phase 4)

GenDI now includes a dedicated BenchmarkDotNet project:

- `tests/GenDI.Benchmarks`

Primary benchmark focus is startup registration cost:

- generated registration (`AddGenDIServices`)
- reflection-based runtime scanning

Latest published benchmark report:

- `docs/BENCHMARKS.md`

## Packaging and CI/CD Baseline (Phase 4 / early Phase 5)

The repository includes:

- `versions.props` for centralized dynamic versioning
- `pack.props` for package metadata and packing defaults
- `.github/workflows/ci-cd.yml` and `.github/workflows/auto-publish.yml` prepared for Sonar/NuGet flows

## Local Tooling and Git Hooks

The repository uses local tools and Husky hooks:

- `dotnet-tools.json` includes `csharpier` and `husky`
- pre-commit runs:
  - `dotnet csharpier format .`
  - `dotnet test`

For fresh clones, `src/GenDI/GenDI.csproj` runs a pre-restore target that executes `dotnet tool restore` and `dotnet husky install`.

---

## Compatibility

| Platform / Framework                     | Status |
|------------------------------------------|--------|
| .NET 8+                                  | YES    |
| NativeAOT                                | YES    |
| Trimming                                 | YES    |
| Microsoft.Extensions.DependencyInjection | YES    |
| ASP.NET Core Minimal API                 | YES    |
| Worker Service / hosted services         | YES    |
| Blazor WebAssembly                       | YES    |
| MAUI / mobile AOT                        | Manual validation recipe |
| F#                                       | Exploration only (no generated `AddGenDIServices()`) |

---

## Roadmap

| Phase | Description                                               | Status     |
|-------|-----------------------------------------------------------|------------|
| 1     | `InjectableAttribute` - attribute-based registration      | Implemented |
| 2     | Attribute model + contract discovery + ordering           | Implemented |
| 3     | Advanced NativeAOT support (ILLink.xml, trimming, AOT)   | Implemented |
| 4     | Benchmarks, website/docs, and CI hardening               | Implemented |
| 5     | Official NuGet publication                                | Implemented |
| 6     | Developer experience and ecosystem expansion              | In Progress |

See the full plan in [ROADMAP.md](ROADMAP.md).

## Phase 6 delivery baseline (single status source)

The canonical track status is maintained in `/home/runner/work/GenDI/GenDI/docs/ROTEIRO_FASE6.md` and mirrored here:

| Track | Status |
|---|---|
| 4.1 Source-generator quality | Delivered |
| 4.2 Registration model (RM-01..RM-12) | Delivered |
| 4.3 Platform/framework support | Delivered |
| 4.4 Testing ergonomics | Delivered |
| 4.5 Explicit registration strategies (Add/TryAdd) | Delivered |
| 4.6 OptionConfig evolution | Delivered |
| 4.7 Tooling/IDE | Pending |
| 4.8 Observability | Pending |
| 4.9 Community/ecosystem | Pending |

Detailed references:
- [docs/ROTEIRO_FASE6.md](docs/ROTEIRO_FASE6.md)
- [docs/REGISTRATION_MODEL_RM01_RM12.md](docs/REGISTRATION_MODEL_RM01_RM12.md)
- [docs/PLATFORM_FRAMEWORK_SUPPORT.md](docs/PLATFORM_FRAMEWORK_SUPPORT.md)

Detailed RM-01..RM-12 documentation:
- [docs/REGISTRATION_MODEL_RM01_RM12.md](docs/REGISTRATION_MODEL_RM01_RM12.md)

Platform/framework notes:
- [docs/PLATFORM_FRAMEWORK_SUPPORT.md](docs/PLATFORM_FRAMEWORK_SUPPORT.md)

---

## Contributing

Contributions are welcome! Please read [CONTRIBUTING.md](CONTRIBUTING.md) before opening a pull request.

---

## License

This project is licensed under the MIT License - see [LICENSE.md](LICENSE.md) for details.
