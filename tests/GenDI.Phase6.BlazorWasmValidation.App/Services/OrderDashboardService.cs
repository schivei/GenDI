using GenDI;

namespace GenDI.Phase6.BlazorWasmValidation.App.Services;

[ServiceInjection]
public interface IOrderDashboardService
{
    string BuildSummary();
}

[Injectable<IOrderDashboardService>(ServiceLifetime.Singleton)]
public sealed class OrderDashboardService : IOrderDashboardService
{
    [GenDI.Inject]
    public required TimeProvider TimeProvider { get; init; }

    public string BuildSummary() => $"Orders dashboard ready at {TimeProvider.GetUtcNow():O}";
}
