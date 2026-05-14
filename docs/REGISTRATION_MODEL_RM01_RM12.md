# Registration Model — RM-01 to RM-12 (Detailed)

This document explains each registration-model item with practical context:

- what it solves
- when to use it
- where it appears in real projects
- a concrete example

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

## RM-01 — `[InjectOptional]` (optional property injection)

### What it solves

Some dependencies are optional (observability adapters, secondary integrations, feature plugins). You want "use if available" behavior without throwing.

### Example

```csharp
[Injectable]
public sealed class OrderPublisher
{
    [Inject] public required IMessageBus Bus { get; init; }

    [InjectOptional]
    public required IAuditSink? Audit { get; init; }
}
```

Generated resolution uses non-throwing `GetService(...)` / `GetKeyedService(...)` semantics.

## RM-02 — `[ConditionalInjectable("Environment")]`

### What it solves

A single codebase often needs different implementations by environment (Dev, Staging, Production).

### Example

```csharp
[Injectable<IPaymentGateway>(ServiceLifetime.Scoped)]
[ConditionalInjectable("Development")]
public sealed class FakePaymentGateway : IPaymentGateway { }

[Injectable<IPaymentGateway>(ServiceLifetime.Scoped)]
[ConditionalInjectable("Production")]
public sealed class StripePaymentGateway : IPaymentGateway { }
```

## RM-03 — `[DecoratorFor<TService>]`

### What it solves

Cross-cutting behavior should be applied around a core implementation without changing the core class.

### Example

```csharp
[Injectable<IInventoryService>(ServiceLifetime.Scoped)]
public sealed class InventoryService : IInventoryService { }

[DecoratorFor<IInventoryService>]
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
```

## RM-04 — `ServiceInjectionAttribute` lifetime fallback

### What it solves

Define default lifetime policy at contract level to avoid inconsistency across many implementations.

### Example

```csharp
[ServiceInjection(ServiceLifetime.Scoped)]
public interface IOrderRepository { }

[Injectable]
public sealed class SqlOrderRepository : IOrderRepository { }
```

Precedence: `Injectable > ServiceInjection > Transient`.

## RM-05 — indirect registration via `[Inject]`

### What it solves

Implementation can be discovered from requested dependency even without direct annotation on implementation.

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

## RM-06 — lifetime override in `[Inject]` + tie-break

### What it solves

Indirect discovery can require explicit lifetime control at dependency request site.

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
Tie-break when competing registrations exist: `Scoped > Singleton > Transient`.

## RM-07 — thread-isolation registration policy

### What it solves

Enable thread-aware reuse behavior for thread-affine or context-sensitive components.

### Example

```csharp
[Injectable<IExecutionContext>(
    ServiceLifetime.Singleton,
    ThreadIsolation = ThreadIsolationPolicy.Scoped)]
public sealed class ExecutionContext : IExecutionContext { }
```

## RM-08 — dependency scanning across referenced libraries

### What it solves

Allows centralized generated registration even when implementations live in referenced solution projects.

### Practical scenario

- `MyApi` references `MyCompany.Orders`
- `MyCompany.Orders` contains `[Injectable]` services
- `MyApi` still calls only `services.AddGenDIServices()`

## RM-09 — inferable closed-generic indirect resolution

### What it solves

Closed generic dependencies (`IRepository<Order>`) can be inferred from open generic implementation patterns when unambiguous.

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

GenDI infers `IRepository<Order> -> EfRepository<Order>`.

## RM-10 — `OptionConfigAttribute` for `IOptions<>`

### What it solves

Reduce repetitive manual binding across many options sections.

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

## RM-11 — static factory registration (`[InjectableFactory<TService>]`)

### What it solves

Centralize complex construction logic (SDK clients, adapters, conditional creation).

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

Supported metadata: `Lifetime`, `Group`, `Order`, `Key`, `ThreadIsolation`, `Module`.

## RM-12 — module grouping/filtering (`[InjectableModule]` + `Module`)

### What it solves

Selective loading of registrations by bounded context.

### Example

```csharp
builder.Services.AddGenDIServices("Billing", "Orders");
```

Generated APIs:

- `AddGenDIServices(IServiceCollection services)`
- `AddGenDIServices(IServiceCollection services, params string[] modules)`

## Open-generic guardrails (`GENDISG001`)

Open-generic paths are intentionally bypassed and warned:

- injectable classes/contracts/decorators
- indirect `[Inject]` discovery
- factory registration flows

This keeps generated output closed-generic and compatible with NativeAOT/trimming constraints.
