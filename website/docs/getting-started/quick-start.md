# Quick Start

This walkthrough shows a minimal but production-shaped setup.

## Step 1: Define contracts

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

## Step 2: Add implementations

```csharp
[Injectable<IClock>(ServiceLifetime.Singleton)]
public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

[Injectable<IInvoiceService>(ServiceLifetime.Scoped, Group = 10, Order = 1, Key = "invoices")]
public sealed class InvoiceService(IClock clock) : IInvoiceService
{
    [Inject(Key = "invoices")]
    public required ILogger<InvoiceService> Logger { get; init; }

    public Task GenerateAsync(Guid invoiceId, CancellationToken ct = default)
    {
        Logger.LogInformation("Generating invoice {Id} at {UtcNow}", invoiceId, clock.UtcNow);
        return Task.CompletedTask;
    }
}
```

## Step 3: Register generated services

```csharp
using MyProject.DependencyInjection;

builder.Services.AddGenDIServices();
```

## Step 4: Consume

```csharp
var service = provider.GetRequiredService<IInvoiceService>();
await service.GenerateAsync(Guid.NewGuid());
```

## Notes

- If no `[ServiceInjection]` contract is found, GenDI falls back to registering the concrete type as its own service.
- Property injection requires `[Inject]` + `get; init;` and public/internal visibility.
- Keyed dependencies can be resolved by `[Inject(Key = ...)]` or constructor `[FromKeyedServices(...)]`.
