using Microsoft.CodeAnalysis;

namespace GenDI.SourceGenerator.Models;

#pragma warning disable S107 // model constructors intentionally capture all immutable registration data
internal sealed class InjectableFactoryMetadata(
    string lifetime,
    string? serviceType,
    ITypeSymbol? serviceTypeSymbol,
    bool hasOpenGenericServiceType,
    int order,
    int group,
    string? keyExpression,
    string? threadIsolationLifetime,
    string? moduleName
)
{
    public string Lifetime { get; } = lifetime;

    public string? ServiceType { get; } = serviceType;

    public ITypeSymbol? ServiceTypeSymbol { get; } = serviceTypeSymbol;

    public bool HasOpenGenericServiceType { get; } = hasOpenGenericServiceType;

    public int Order { get; } = order;

    public int Group { get; } = group;

    public string? KeyExpression { get; } = keyExpression;

    public string? ThreadIsolationLifetime { get; } = threadIsolationLifetime;

    public string? ModuleName { get; } = moduleName;
}
#pragma warning restore S107
