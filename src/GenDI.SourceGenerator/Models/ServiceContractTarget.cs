namespace GenDI.SourceGenerator.Models;

internal sealed class ServiceContractTarget(
    string serviceType,
    string? fallbackLifetime,
    string? fallbackThreadIsolationLifetime,
    bool? fallbackAllowMultiple,
    bool? fallbackUseTryAdd
)
{
    public string ServiceType { get; } = serviceType;

    public string? FallbackLifetime { get; } = fallbackLifetime;

    public string? FallbackThreadIsolationLifetime { get; } = fallbackThreadIsolationLifetime;

    public bool? FallbackAllowMultiple { get; } = fallbackAllowMultiple;

    public bool? FallbackUseTryAdd { get; } = fallbackUseTryAdd;
}
