# Quick Start

This walkthrough builds a minimal but production-shaped setup using GenDI's idiomatic **property injection** style.

## Step 1: Define contracts

Mark the interfaces you want injected with `[ServiceInjection]`:

```csharp
[ServiceInjection]
public interface IInvoiceService
{
    Task GenerateAsync(Guid invoiceId, CancellationToken ct = default);
}

[ServiceInjection]
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
```

## Step 2: Implement with property injection

Declare dependencies as `required` init-only properties. No constructor needed:

```csharp
[Injectable<IClock>(ServiceLifetime.Singleton)]
public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

[Injectable<IInvoiceService>(ServiceLifetime.Scoped, Group = 10, Order = 1)]
public sealed class InvoiceService : IInvoiceService
{
    [Inject] public required IClock Clock { get; init; }
    [Inject] public required ILogger<InvoiceService> Logger { get; init; }

    public Task GenerateAsync(Guid invoiceId, CancellationToken ct = default)
    {
        Logger.LogInformation("Generating invoice {Id} at {UtcNow}", invoiceId, Clock.UtcNow);
        return Task.CompletedTask;
    }
}
```

Each `[Inject]` property replaces a constructor parameter + a private field + an assignment — all at once.

## Step 3: Register generated services

One call wires everything GenDI discovered at compile time:

```csharp
using MyProject.DependencyInjection;

builder.Services.AddGenDIServices();
```

## Step 4: Consume

```csharp
var service = provider.GetRequiredService<IInvoiceService>();
await service.GenerateAsync(Guid.NewGuid());
```

## Property injection vs constructor injection at a glance

Both styles work. Property injection shines as the number of dependencies grows:

```csharp
// Constructor style — each new dependency touches three lines
public InvoiceService(IClock clock, ILogger<InvoiceService> logger)
{
    _clock = clock;   // private field
    _logger = logger; // private field
}

// Property style — one line per dependency, zero private fields
[Inject] public required IClock Clock { get; init; }
[Inject] public required ILogger<InvoiceService> Logger { get; init; }
```

## Keyed services

Use `Key` on `[Injectable]` and `[Inject]` to work with keyed registrations:

```csharp
[Injectable<IInvoiceService>(ServiceLifetime.Scoped, Key = "invoices")]
public sealed class InvoiceService : IInvoiceService
{
    [Inject(Key = "invoices")] public required ILogger<InvoiceService> Logger { get; init; }
}

// Resolve by key:
var service = provider.GetRequiredKeyedService<IInvoiceService>("invoices");
```

## Notes

- If no `[ServiceInjection]` contract is found, GenDI falls back to registering the concrete type as its own service.
- `[Inject]` properties must be `get; init;` with public or internal visibility.
- Constructor injection with `[FromKeyedServices]` is also fully supported alongside property injection.
