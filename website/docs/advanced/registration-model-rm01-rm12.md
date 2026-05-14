---
sidebar_position: 5
---

# RM-01 to RM-12 — Detailed registration model notes

This page explains each registration-model item with:

- what it solves
- when to use it
- where it usually appears in real projects
- a concrete example

> If you are evaluating GenDI for production adoption, this is the reference page for real-world usage decisions.

## Quick decision map

| Need | Use |
|---|---|
| Optional/non-critical dependency | `RM-01 [InjectOptional]` |
| Different implementations per environment | `RM-02 [ConditionalInjectable]` |
| Wrap service behavior (logging, caching, retry) | `RM-03 [DecoratorFor<T>]` |
| Define contract lifetime fallback once | `RM-04 [ServiceInjection(...)]` |
| Auto-discover implementation from dependency graph | `RM-05 / RM-06 [Inject]` indirect registration |
| Thread-aware reuse policy | `RM-07 ThreadIsolation` |
| Discover services in referenced projects | `RM-08 cross-assembly scanning` |
| Infer closed generic from open implementation | `RM-09 closed-generic inference` |
| Bind config to `IOptions<T>` automatically | `RM-10 [OptionConfig]` |
| Centralized service creation logic | `RM-11 [InjectableFactory<T>]` |
| Load only selected bounded contexts | `RM-12 modules` |

---

## RM-01 — `[InjectOptional]`

### What it solves

Some dependencies are optional (observability adapters, secondary integrations, feature plugins). You want "use if available" behavior without throwing.

### When to use

- plugin-style extensions
- telemetry/audit sinks
- non-critical integrations

### Example

```csharp
[Injectable]
public sealed class OrderPublisher
{
    [Inject] public required IMessageBus Bus { get; init; }

    [InjectOptional]
    public required IAuditSink? Audit { get; init; }

    public async Task PublishAsync(Order order, CancellationToken ct)
    {
        await Bus.PublishAsync(order, ct);
        await (Audit?.WriteAsync($"Order {order.Id} published") ?? Task.CompletedTask);
    }
}
```

---

## RM-02 — `ConditionalInjectable(environmentName)`

### What it solves

A single codebase often needs different implementations by environment (Dev, Staging, Production).

### When to use

- fake gateway in development
- production-only external adapter
- sandbox vs live integrations

### Example

```csharp
[Injectable<IPaymentGateway>(ServiceLifetime.Scoped)]
[ConditionalInjectable("Development")]
public sealed class FakePaymentGateway : IPaymentGateway
{
    public Task ChargeAsync(Guid orderId, CancellationToken ct = default) => Task.CompletedTask;
}

[Injectable<IPaymentGateway>(ServiceLifetime.Scoped)]
[ConditionalInjectable("Production")]
public sealed class StripePaymentGateway : IPaymentGateway
{
    public Task ChargeAsync(Guid orderId, CancellationToken ct = default) => Task.CompletedTask;
}
```

---

## RM-03 — `DecoratorFor<TService>` / `DecoratorFor(Order = ...)`

### What it solves

Cross-cutting behavior should not pollute core service logic.

### When to use

- logging
- metrics
- retry/circuit-breaker wrappers
- authorization checks around an existing service

### Example

```csharp
[ServiceInjection]
public interface IInventoryService;

[Injectable(ServiceLifetime.Scoped)]
public sealed class InventoryService : IInventoryService
{
    public Task ReserveAsync(Guid orderId, CancellationToken ct = default) => Task.CompletedTask;
}

[DecoratorFor<IInventoryService>(Order = 0)]
public sealed class InventoryLoggingDecorator(
    IInventoryService inner,
    ILogger<InventoryLoggingDecorator> logger) : IInventoryService
{
    public async Task ReserveAsync(Guid orderId, CancellationToken ct = default)
    {
        logger.LogInformation("Reserve start {OrderId}", orderId);
        await inner.ReserveAsync(orderId, ct);
        logger.LogInformation("Reserve end {OrderId}", orderId);
    }
}

[DecoratorFor(Order = 1)]
public sealed class InventoryValidationDecorator : IInventoryService
{
    [Inject] public required IInventoryService Inner { get; init; }
}
```

Decorator pipelines are generated statically in ascending `Order`; ties fall back to ordinal
decorator type-name ordering. The non-generic attribute resolves exactly one
`[ServiceInjection]` contract from the decorator inheritance/implementation chain. Every decorator
must expose a public constructor parameter or `[Inject]` init-only property matching that
contract, otherwise the analyzer fails the build.

---

## RM-04 — `ServiceInjection` lifetime fallback

### What it solves

Large systems with many implementations of the same contract can become inconsistent in lifetime choices.

### When to use

- define team-wide default lifetime at contract level
- keep implementations concise

### Example

```csharp
[ServiceInjection(ServiceLifetime.Scoped)]
public interface IOrderRepository { }

[Injectable]
public sealed class SqlOrderRepository : IOrderRepository { }
```

Fallback precedence remains: `Injectable > ServiceInjection > Transient`.

---

## RM-05 — indirect registration from `[Inject]`

### What it solves

Sometimes you consume a contract, but the implementation itself is not explicitly annotated. GenDI can infer and register it through usage.

### When to use

- migration phases from manual DI
- internal implementations where direct annotation is not practical

### Example

```csharp
public interface ITokenSigner { }
public sealed class JwtTokenSigner : ITokenSigner { }

[Injectable]
public sealed class AuthService
{
    [Inject]
    public required ITokenSigner TokenSigner { get; init; }
}
```

---

## RM-06 — lifetime override in `[Inject]`

### What it solves

Indirect discovery can require explicit lifetime control at dependency request site.

### When to use

- same contract consumed in different composition needs
- override inferred/default lifetime for specific graph semantics

### Example

```csharp
[Injectable]
public sealed class ReportService
{
    [Inject(ServiceLifetime.Scoped)]
    public required IReportFormatter Formatter { get; init; }
}
```

Precedence: `Inject > Injectable > ServiceInjection > Transient`.
Tie-break: `Scoped > Singleton > Transient`.

---

## RM-07 — thread-isolation policy

### What it solves

Some scenarios require thread-local reuse behavior to avoid sharing state unexpectedly.

### When to use

- thread-affine resources
- context-sensitive caches
- specialized parallel workloads

### Example

```csharp
[Injectable<IExecutionContext>(
    ServiceLifetime.Singleton,
    ThreadIsolation = ThreadIsolationPolicy.Scoped)]
public sealed class ExecutionContext : IExecutionContext { }
```

---

## RM-08 — scanning across referenced libraries

### What it solves

Monoliths and modular solutions often place implementations in separate projects.

### When to use

- layered architecture (`Api -> Application -> Infrastructure`)
- shared business modules referenced by multiple hosts

### Typical layout

- `MyApi` references `MyCompany.Orders`
- `MyCompany.Orders` contains `[Injectable]` services
- `MyApi` still uses only `services.AddGenDIServices()`

This keeps composition root clean while preserving compile-time generation.

---

## RM-09 — closed-generic indirect inference

### What it solves

Closed generic dependencies are frequent (`IRepository<Order>`). GenDI can infer them from open generic implementation patterns when the mapping is unambiguous.

### When to use

- repository/specification patterns
- generic handlers and adapters

### Example

```csharp
public interface IRepository<T> { }
public sealed class EfRepository<T> : IRepository<T> { }

[Injectable]
public sealed class OrderReadService
{
    [Inject]
    public required IRepository<Order> Orders { get; init; }
}
```

GenDI infers `IRepository<Order> -> EfRepository<Order>` in generated output.

---

## RM-10 — `OptionConfigAttribute` + `IOptions<>`

### What it solves

Configuration binding is repetitive when done manually across many option types.

### When to use

- multiple options sections
- apps with many feature flags/settings blocks

### Example

```csharp
[OptionConfig("Payments:Stripe")]
public sealed class StripeOptions
{
    public required string ApiKey { get; init; }
    public bool EnableRetries { get; init; }
}

[Injectable]
public sealed class StripeClient
{
    [Inject]
    public required IOptions<StripeOptions> Options { get; init; }
}
```

---

## RM-11 — factory registration (`[InjectableFactory<TService>]`)

### What it solves

Some dependencies require controlled creation logic (third-party SDK builder, conditional setup, precomputed objects).

### When to use

- construction cannot be represented by simple `new`
- centralize creation and keep services lean

### Example

```csharp
[InjectableModule("Billing")]
public static class BillingFactories
{
    [InjectableFactory<IPaymentGateway>(
        ServiceLifetime.Singleton,
        Key = "stripe",
        Module = "Billing")]
    public static IPaymentGateway CreateStripeGateway() => new StripePaymentGateway();
}
```

Metadata supported: `Lifetime`, `Group`, `Order`, `Key`, `ThreadIsolation`, `Module`.

---

## RM-12 — module grouping (`[InjectableModule]` + `Module`)

### What it solves

Large applications need selective loading by bounded context.

### When to use

- plugin architecture
- feature toggles by module
- hosts that load only subsets of shared components

### Example

```csharp
builder.Services.AddGenDIServices("Billing", "Orders");
```

This loads only registrations associated with selected modules.

---

## Open-generic bypass and warning `GENDISG001`

Open-generic generation paths are intentionally bypassed and not registered. GenDI emits warning `GENDISG001` so the omission is explicit.

Where this applies:

- injectable classes/contracts/decorators
- indirect `[Inject]` discovery paths
- factory registrations

This keeps generated output closed-generic and safe for NativeAOT/trimming scenarios.
