# 🚀 Quick Start

This guide shows a **realistic API slice** (orders + payments + notifications) using GenDI in the way most teams structure production code.

## Scenario

You are building an Orders API. For each order, you need to:

1. validate inventory
2. charge payment
3. persist data
4. send notification

Instead of manual `AddScoped<...>()` wiring and constructor boilerplate, GenDI will generate registrations and activation code.

## 🎯 Step 1: Define contracts

Mark contracts with `[ServiceInjection]` so GenDI can discover them across implementations.

```csharp
[ServiceInjection]
public interface IOrderService
{
    Task PlaceOrderAsync(Guid orderId, CancellationToken ct = default);
}

[ServiceInjection(ServiceLifetime.Singleton)]
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

[ServiceInjection]
public interface IPaymentGateway
{
    Task ChargeAsync(Guid orderId, CancellationToken ct = default);
}

[ServiceInjection]
public interface INotificationService
{
    Task NotifyAsync(Guid orderId, CancellationToken ct = default);
}
```

## 🏗️ Step 2: Implement services with property injection

Use `[Injectable]` for registration and `[Inject]` for dependencies.

```csharp
[Injectable<IClock>(ServiceLifetime.Singleton)]
public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

[Injectable<IPaymentGateway>(ServiceLifetime.Scoped, Module = "Billing")]
public sealed class StripeGateway : IPaymentGateway
{
    [Inject] public required ILogger<StripeGateway> Logger { get; init; }

    public Task ChargeAsync(Guid orderId, CancellationToken ct = default)
    {
        Logger.LogInformation("Charging order {OrderId}", orderId);
        return Task.CompletedTask;
    }
}

[Injectable<INotificationService>(ServiceLifetime.Scoped)]
public sealed class EmailNotificationService : INotificationService
{
    public Task NotifyAsync(Guid orderId, CancellationToken ct = default) => Task.CompletedTask;
}

[Injectable<IOrderService>(ServiceLifetime.Scoped, Group = 10, Order = 1)]
public sealed class OrderService : IOrderService
{
    [Inject] public required IClock Clock { get; init; }
    [Inject] public required IPaymentGateway PaymentGateway { get; init; }
    [Inject] public required INotificationService NotificationService { get; init; }
    [Inject] public required ILogger<OrderService> Logger { get; init; }

    public async Task PlaceOrderAsync(Guid orderId, CancellationToken ct = default)
    {
        Logger.LogInformation("Processing order {OrderId} at {UtcNow}", orderId, Clock.UtcNow);
        await PaymentGateway.ChargeAsync(orderId, ct);
        await NotificationService.NotifyAsync(orderId, ct);
    }
}
```

Why this scales better:

- each dependency is one `[Inject]` property
- class shape stays readable as dependencies grow
- generated activation remains explicit and debuggable

## ⚡ Step 3: Register generated services

```csharp
using MyProject.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGenDIServices();
```

That single call includes all services discovered by GenDI (including referenced projects).

## ▶️ Step 4: Consume in endpoint/controller

Assumindo que `var app = builder.Build();` já foi executado antes deste trecho:

```csharp
app.MapPost("/orders/{id:guid}", async (Guid id, IOrderService orders, CancellationToken ct) =>
{
    await orders.PlaceOrderAsync(id, ct);
    return Results.Accepted();
});
```

## 🔧 Practical variants you will use in real projects

### Optional dependency (`[InjectOptional]`)

Use when the feature is non-critical (e.g., telemetry plugin):

```csharp
[Injectable]
public sealed class OrderAudit
{
    [InjectOptional]
    public required IAuditSink? AuditSink { get; init; }
}
```

### Environment-specific implementation (`[ConditionalInjectable]`)

Use different implementations for Development/Production:

```csharp
[Injectable<IPaymentGateway>(ServiceLifetime.Scoped)]
[ConditionalInjectable("Development")]
public sealed class FakeGateway : IPaymentGateway
{
    public Task ChargeAsync(Guid orderId, CancellationToken ct = default) => Task.CompletedTask;
}
```

### Module filtering (`[InjectableModule]` + `Module`)

Use when you need selective registration by bounded context:

```csharp
builder.Services.AddGenDIServices("Billing", "Orders");
```

When module filters are provided, registrations without `Module` are omitted from the generated call.
In the example above, `EmailNotificationService` and `OrderService` are only included when using `AddGenDIServices()` without module filters (or after assigning them to a module).

## 📝 Common decisions

- **Use `[Injectable<TService>]`** when you want explicit contract mapping.
- **Use plain `[Injectable]`** for self-registration or when contract comes from `[ServiceInjection]`.
- **Use `Group`/`Order`** when deterministic registration order matters.
- **Use `Key`** for multi-implementation scenarios (tenant, provider, channel).

For detailed advanced behavior (RM-01..RM-12), see:

- [RM-01 to RM-12 — Detailed registration model notes](../advanced/registration-model-rm01-rm12)
