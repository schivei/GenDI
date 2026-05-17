// ReSharper disable NotAccessedPositionalProperty.Global
namespace GenDI.Phase6.MinimalApiValidation.App;

[ServiceInjection]
public interface IOrderEndpointService
{
    OrderResponse Create(Guid orderId);
}

public sealed record OrderResponse(Guid OrderId, string Status, DateTimeOffset GeneratedAtUtc);

[Injectable<IOrderEndpointService>(ServiceLifetime.Scoped)]
public sealed class OrderEndpointService : IOrderEndpointService
{
    [Inject]
    public required ILogger<OrderEndpointService> Logger { get; init; }

    [Inject]
    public required TimeProvider TimeProvider { get; init; }

    public OrderResponse Create(Guid orderId)
    {
        if (Logger.IsEnabled(LogLevel.Information))
        {
            Logger.LogInformation("Generating response for order {OrderId}", orderId);
        }

        return new OrderResponse(orderId, "accepted", TimeProvider.GetUtcNow());
    }
}
