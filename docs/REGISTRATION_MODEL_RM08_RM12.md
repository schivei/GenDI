# Registration Model — RM-01 to RM-12 (Detailed)

This document consolidates **all delivered registration-model items (RM-01..RM-12)** with practical usage-oriented examples.

## RM-01 — `[InjectOptional]` (optional property injection)

Use `[InjectOptional]` when missing registration must not throw:

```csharp
[Injectable]
public sealed class UsesOptional
{
    [InjectOptional]
    public required IAuditService? Audit { get; init; }
}
```

Generated resolution uses `GetService(...)` / `GetKeyedService(...)` semantics.

## RM-02 — `[ConditionalInjectable("Environment")]`

Register only in matching runtime environment (`DOTNET_ENVIRONMENT` / `ASPNETCORE_ENVIRONMENT`):

```csharp
[Injectable<IMyService>(ServiceLifetime.Singleton)]
[ConditionalInjectable("Development")]
public sealed class DevService : IMyService { }
```

## RM-03 — `[DecoratorFor<TService>]`

Wrap a previously registered contract:

```csharp
[Injectable<IMyService>]
public sealed class CoreService : IMyService { }

[DecoratorFor<IMyService>]
public sealed class LoggingDecorator(IMyService inner) : IMyService { }
```

GenDI rewrites registration to resolve `CoreService` and return `LoggingDecorator`.

## RM-04 — `ServiceInjectionAttribute` lifetime fallback

Contract-level fallback is used when implementation does not define a stronger lifetime:

```csharp
[ServiceInjection(ServiceLifetime.Scoped)]
public interface IContract { }

[Injectable]
public sealed class Implementation : IContract { }
```

Precedence: `Injectable > ServiceInjection > Transient`.

## RM-05 — indirect registration via `[Inject]`

Implementation can be discovered from requested dependency even without `[Injectable]`:

```csharp
public interface IContract { }
public sealed class ContractImpl : IContract { }

[Injectable]
public sealed class Consumer
{
    [Inject]
    public required IContract Contract { get; init; }
}
```

## RM-06 — lifetime override in `[Inject]` + tie-break

Dependency request can force indirect registration lifetime:

```csharp
[Inject(ServiceLifetime.Scoped)]
public required IContract Contract { get; init; }
```

Precedence: `Inject > Injectable > ServiceInjection > Transient`  
Tie-break when competing registrations exist: `Scoped > Singleton > Transient`.

## RM-07 — thread-isolation registration policy

Configure thread-local resolution cache:

```csharp
[Injectable<IContract>(ServiceLifetime.Singleton, ThreadIsolation = ThreadIsolationPolicy.Scoped)]
public sealed class ThreadAwareService : IContract { }
```

Supported policies map to singleton/scoped/transient registration strategies.

## RM-08 — dependency scanning across referenced libraries

GenDI scans referenced solution assemblies (excluding framework/test assemblies), enabling centralized registration even when implementations live in other projects.

Practical scenario:
- `WebApp` references `Domain.Services`
- `Domain.Services` contains `[Injectable]` types
- `WebApp` still calls only `services.AddGenDIServices()`

## RM-09 — inferable closed-generic indirect resolution

Closed contracts are inferred from open implementations when mapping is unambiguous:

```csharp
public interface IRepository<T> { }
public sealed class Repository<T> : IRepository<T> { }

[Injectable]
public sealed class UsesRepo
{
    [Inject]
    public required IRepository<Order> Repository { get; init; }
}
```

GenDI builds registration for `IRepository<Order> -> Repository<Order>`.

## RM-10 — `OptionConfigAttribute` for `IOptions<>`

Bind options via configuration path:

```csharp
[OptionConfig("Features:MyFeature")]
public sealed class MyFeatureOptions
{
    public bool Enabled { get; init; }
}

[Injectable]
public sealed class UsesOptions
{
    [Inject]
    public required IOptions<MyFeatureOptions> Options { get; init; }
}
```

Generated code uses `GetSection(...)` + `ConfigurationBinder.Get<T>()`.

## RM-11 — static factory registration (`[InjectableFactory]` / `[InjectableFactory<TService>]`)

Preferred explicit contract style:

```csharp
public static class BillingFactories
{
    [InjectableFactory<IPaymentGateway>(ServiceLifetime.Singleton)]
    public static IPaymentGateway Create() => new StripePaymentGateway();
}
```

Supported metadata: `Lifetime`, `Group`, `Order`, `Key`, `ThreadIsolation`, `Module`.

## RM-12 — module grouping/filtering (`[InjectableModule]` + `Module`)

Group registrations and load selectively:

```csharp
[InjectableModule("Billing")]
public static class BillingFactories
{
    [InjectableFactory<IPaymentGateway>(Module = "Billing")]
    public static IPaymentGateway Create() => new StripePaymentGateway();
}
```

Generated APIs:
- `AddGenDIServices(IServiceCollection services)`
- `AddGenDIServices(IServiceCollection services, params string[] modules)`

## Open-generic guardrails (`GENDISG001`)

Open-generic paths are intentionally bypassed and warned:
- injectable classes/contracts/decorators
- indirect `[Inject]` discovery
- factory registration flows

This follows the Phase 6 NativeAOT constraint: registration output must remain closed-generic.
