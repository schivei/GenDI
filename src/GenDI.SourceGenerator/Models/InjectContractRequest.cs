using Microsoft.CodeAnalysis;

namespace GenDI.SourceGenerator.Models;

#pragma warning disable S107 // model constructors intentionally capture all immutable registration data
internal sealed class InjectContractRequest(
    INamedTypeSymbol contractSymbol,
    string serviceType,
    string? keyExpression,
    string? lifetimeOverride,
    bool? allowMultipleOverride,
    bool? useTryAddOverride,
    string? moduleName
)
{
    public INamedTypeSymbol ContractSymbol { get; } = contractSymbol;

    public string ServiceType { get; } = serviceType;

    public string? KeyExpression { get; } = keyExpression;

    public string? LifetimeOverride { get; } = lifetimeOverride;

    public bool? AllowMultipleOverride { get; } = allowMultipleOverride;

    public bool? UseTryAddOverride { get; } = useTryAddOverride;

    public string? ModuleName { get; } = moduleName;
}
#pragma warning restore S107
