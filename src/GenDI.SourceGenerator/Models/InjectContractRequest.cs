using Microsoft.CodeAnalysis;

namespace GenDI.SourceGenerator;

#pragma warning disable S107 // model constructors intentionally capture all immutable registration data
internal sealed class InjectContractRequest
{
    public InjectContractRequest(
        INamedTypeSymbol contractSymbol,
        string serviceType,
        string? keyExpression,
        string? lifetimeOverride,
        bool? allowMultipleOverride,
        bool? useTryAddOverride,
        string? moduleName
    )
    {
        ContractSymbol = contractSymbol;
        ServiceType = serviceType;
        KeyExpression = keyExpression;
        LifetimeOverride = lifetimeOverride;
        AllowMultipleOverride = allowMultipleOverride;
        UseTryAddOverride = useTryAddOverride;
        ModuleName = moduleName;
    }

    public INamedTypeSymbol ContractSymbol { get; }

    public string ServiceType { get; }

    public string? KeyExpression { get; }

    public string? LifetimeOverride { get; }

    public bool? AllowMultipleOverride { get; }

    public bool? UseTryAddOverride { get; }

    public string? ModuleName { get; }
}
#pragma warning restore S107
