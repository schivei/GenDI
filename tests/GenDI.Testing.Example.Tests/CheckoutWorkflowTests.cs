using System;
using System.Collections.Generic;
using GenDI.Testing.Example.Tests.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

[assembly: GenDI.GenDiCoveration(false)]

namespace GenDI.Testing.Example.Tests;

public class CheckoutWorkflowTests
{
    [Fact]
    public void Checkout_uses_generated_property_injection_with_servicebuilder()
    {
        var productCatalog = new InMemoryProductCatalog(
            new Dictionary<string, decimal> { ["book"] = 40m, ["pen"] = 5m }
        );
        var fixedClock = new FixedClock(new DateTimeOffset(2026, 5, 16, 8, 30, 0, TimeSpan.Zero));

        var builder = ServiceBuilder
            .Create()
            .AddGenDi(services => services.AddGenDIServices())
            .AddSingleton<IProductCatalog>(productCatalog)
            .AddSingleton<ISystemClock>(fixedClock);

        using var provider = builder.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var checkout = scope.ServiceProvider.GetRequiredService<ICheckoutService>();

        var result = checkout.Checkout("book", 2);

        Assert.Equal(80m, result.TotalAmount);
        Assert.Equal(fixedClock.UtcNow, result.ProcessedAt);
    }
}

[ServiceInjection]
public interface ICheckoutService
{
    CheckoutResult Checkout(string sku, int quantity);
}

[Injectable<ICheckoutService>(ServiceLifetime.Scoped)]
public sealed class CheckoutService : ICheckoutService
{
    [Inject]
    public required IProductCatalog ProductCatalog { get; init; }

    [Inject]
    public required ISystemClock Clock { get; init; }

    public CheckoutResult Checkout(string sku, int quantity)
    {
        var unitPrice = ProductCatalog.GetPrice(sku);
        return new CheckoutResult(unitPrice * quantity, Clock.UtcNow);
    }
}

public sealed record CheckoutResult(decimal TotalAmount, DateTimeOffset ProcessedAt);

public interface IProductCatalog
{
    decimal GetPrice(string sku);
}

public sealed class InMemoryProductCatalog(IReadOnlyDictionary<string, decimal> prices)
    : IProductCatalog
{
    public decimal GetPrice(string sku) => prices[sku];
}

public interface ISystemClock
{
    DateTimeOffset UtcNow { get; }
}

public sealed class FixedClock(DateTimeOffset utcNow) : ISystemClock
{
    public DateTimeOffset UtcNow { get; } = utcNow;
}
