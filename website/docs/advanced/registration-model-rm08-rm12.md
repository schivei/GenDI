---
sidebar_position: 5
---

# RM-01 to RM-12 — Detailed registration model notes

This page now consolidates **RM-01..RM-12** with practical examples for real usage.

## RM-01 — `InjectOptional`

```csharp
[Injectable]
public sealed class UsesOptional
{
    [InjectOptional]
    public required IAuditService? Audit { get; init; }
}
```

Optional dependencies are resolved with non-throwing semantics.

## RM-02 — `ConditionalInjectable(environmentName)`

```csharp
[Injectable<IMyService>(ServiceLifetime.Singleton)]
[ConditionalInjectable("Development")]
public sealed class DevService : IMyService { }
```

Registration happens only when runtime environment matches.

## RM-03 — `DecoratorFor<TService>`

```csharp
[Injectable<IMyService>]
public sealed class CoreService : IMyService { }

[DecoratorFor<IMyService>]
public sealed class LoggingDecorator(IMyService inner) : IMyService { }
```

## RM-04 — `ServiceInjection` lifetime fallback

```csharp
[ServiceInjection(ServiceLifetime.Scoped)]
public interface IContract { }

[Injectable]
public sealed class Implementation : IContract { }
```

Fallback precedence: `Injectable > ServiceInjection > Transient`.

## RM-05 — indirect registration from `[Inject]`

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

## RM-06 — lifetime override with `[Inject]`

```csharp
[Inject(ServiceLifetime.Scoped)]
public required IContract Contract { get; init; }
```

Precedence: `Inject > Injectable > ServiceInjection > Transient`  
Tie-break: `Scoped > Singleton > Transient`.

## RM-07 — thread-isolation policy

```csharp
[Injectable<IContract>(ServiceLifetime.Singleton, ThreadIsolation = ThreadIsolationPolicy.Scoped)]
public sealed class ThreadAwareService : IContract { }
```

## RM-08 — Dependency scanning across referenced libraries

- GenDI scans referenced libraries (excluding framework/test assemblies) for injectable types.
- This allows centralized generated registration across solution boundaries.

## RM-09 — Closed-generic indirect inference

- `[Inject]` indirect discovery supports inferable closed-generic implementations.
- Open-generic registrations remain unsupported in generated output.

## RM-10 — `OptionConfigAttribute` + `IOptions<>`

- Mark options with `[OptionConfig("Section:Path")]`.
- GenDI generates `IOptions<T>` binding from `IConfiguration`.

## RM-11 — `[InjectableFactory]` on static methods

- Static factories can define generated registrations with metadata:
  - lifetime
  - key
  - group/order
  - module
  - thread isolation

## RM-12 — `[InjectableModule]` grouped registrations

- Group registrations by module and load selectively:
  - `AddGenDIServices()`
  - `AddGenDIServices("ModuleA", "ModuleB")`

## Open-generic bypass and warnings

- Open-generic generation paths are bypassed and not registered.
- GenDI emits warning `GENDISG001` when bypassing open-generic candidates.
- Applies to injectable classes/contracts/decorators, indirect contracts, and factory registrations.
