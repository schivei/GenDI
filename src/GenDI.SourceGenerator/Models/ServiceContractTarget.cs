namespace GenDI.SourceGenerator;

internal sealed class ServiceContractTarget
{
    public ServiceContractTarget(
        string serviceType,
        string? fallbackLifetime,
        string? fallbackThreadIsolationLifetime,
        bool? fallbackAllowMultiple,
        bool? fallbackUseTryAdd
    )
    {
        ServiceType = serviceType;
        FallbackLifetime = fallbackLifetime;
        FallbackThreadIsolationLifetime = fallbackThreadIsolationLifetime;
        FallbackAllowMultiple = fallbackAllowMultiple;
        FallbackUseTryAdd = fallbackUseTryAdd;
    }

    public string ServiceType { get; }

    public string? FallbackLifetime { get; }

    public string? FallbackThreadIsolationLifetime { get; }

    public bool? FallbackAllowMultiple { get; }

    public bool? FallbackUseTryAdd { get; }
}
