# GenDI

> **Generator-based Dependency Injection for .NET — elegant, fast, AOT-ready**

[![CI/CD Pipeline](https://github.com/schivei/GenDI/actions/workflows/ci-cd.yml/badge.svg)](https://github.com/schivei/GenDI/actions/workflows/ci-cd.yml)
[![Deploy Documentation](https://github.com/schivei/GenDI/actions/workflows/deploy-docs.yml/badge.svg)](https://github.com/schivei/GenDI/actions/workflows/deploy-docs.yml)
[![NuGet GenDI](https://img.shields.io/nuget/v/GenDI.svg)](https://www.nuget.org/packages/GenDI)
[![NuGet GenDI.SourceGenerator](https://img.shields.io/nuget/v/GenDI.SourceGenerator.svg)](https://www.nuget.org/packages/GenDI.SourceGenerator)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://github.com/schivei/GenDI/blob/main/LICENSE.md)
[![Documentation](https://img.shields.io/badge/docs-website-blue)](https://elton.schivei.nom.br/GenDI)

GenDI is a dependency injection library built on top of C# *source generators*, providing full compatibility with NativeAOT and trimming. It works as an additional module to `Microsoft.Extensions.DependencyInjection`, allowing you to register services automatically at compile time — no reflection required.

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
- **No runtime scanning cost**: compile-time generation eliminates startup overhead from reflection-based scanning.
- **AOT/trimming friendly by design**: safe path for teams that need NativeAOT, without forcing this concern for every project.

---

## Installation

```bash
dotnet add package GenDI
```

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

`InjectableAttribute` supports:

- `Lifetime` (constructor argument, default `Transient`)
- `Group` (optional, default `int.MaxValue`)
- `Order` (optional, default `int.MaxValue`)
- `ServiceType`:
  - `[Injectable]` -> `null` (no explicit contract)
  - `[Injectable<TService>]` -> `typeof(TService)` as explicit contract (additive with `[ServiceInjection]`)
- `Key` (optional, default `null`) for keyed service registration generation

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

Constructor injection is also supported and can use the native DI attribute:

```csharp
public MyConsumer([FromKeyedServices("primary")] IMyService service) { }
```

### Analyzer diagnostics (`GenDI.Analyzers`)

`GenDI.Analyzers` currently publishes:

- `GENDI001` — `[Inject]` requires `init`-only property
- `GENDI002` — `[Injectable]` requires concrete non-abstract class
- `GENDI003` — constructor injection can be converted to GenDI property injection (code-fix available)

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

| Platform / Framework                         | Supported |
|----------------------------------------------|-----------|
| .NET 8+                                      | YES       |
| NativeAOT                                    | YES       |
| Trimming                                     | YES       |
| Microsoft.Extensions.DependencyInjection     | YES       |

---

## Roadmap

| Phase | Description                                               | Status     |
|-------|-----------------------------------------------------------|------------|
| 1     | `InjectableAttribute` - attribute-based registration      | Implemented |
| 2     | Attribute model + contract discovery + ordering           | Implemented |
| 3     | Advanced NativeAOT support (ILLink.xml, trimming, AOT)   | Implemented |
| 4     | Benchmarks, website/docs, and CI hardening               | Implemented |
| 5     | Official NuGet publication                                | In Progress |

See the full plan in [ROADMAP.md](ROADMAP.md).

---

## Contributing

Contributions are welcome! Please read [CONTRIBUTING.md](CONTRIBUTING.md) before opening a pull request.

---

## License

This project is licensed under the MIT License - see [LICENSE.md](LICENSE.md) for details.
