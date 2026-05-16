namespace GenDI.SourceGenerator;

internal sealed class ServiceContractTarget
{
    public ServiceContractTarget(
        string serviceType,
        string? fallbackLifetime,
        string? fallbackThreadIsolationLifetime
    )
    {
        ServiceType = serviceType;
        FallbackLifetime = fallbackLifetime;
        FallbackThreadIsolationLifetime = fallbackThreadIsolationLifetime;
    }

    public string ServiceType { get; }

    public string? FallbackLifetime { get; }

    public string? FallbackThreadIsolationLifetime { get; }
}
