using GenDI.Testing;
using GenDI.Testing.Example.Tests.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

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
            .AddSingleton<IProductCatalog>(productCatalog)
            .AddSingleton<ISystemClock>(fixedClock)
            .AddGenDI(services => services.AddGenDIServices());

        using var provider = builder.BuildServiceProvider();
        var checkout = provider.GetRequiredService<ICheckoutService>();

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
