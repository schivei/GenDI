using Microsoft.CodeAnalysis;

namespace GenDI.SourceGenerator.Models;

#pragma warning disable S107 // model constructors intentionally capture all immutable registration data
internal sealed class InjectableMetadata(
    string lifetime,
    string? explicitServiceType,
    ITypeSymbol? explicitServiceTypeSymbol,
    bool hasOpenGenericExplicitServiceType,
    int order,
    int group,
    bool? allowMultiple,
    bool? useTryAdd,
    string? keyExpression,
    string? threadIsolationLifetime,
    string? moduleName
)
{
    public string Lifetime { get; } = lifetime;

    public string? ExplicitServiceType { get; } = explicitServiceType;

    public ITypeSymbol? ExplicitServiceTypeSymbol { get; } = explicitServiceTypeSymbol;

    public bool HasOpenGenericExplicitServiceType { get; } = hasOpenGenericExplicitServiceType;

    public int Order { get; } = order;

    public int Group { get; } = group;

    public bool? AllowMultiple { get; } = allowMultiple;

    public bool? UseTryAdd { get; } = useTryAdd;

    public string? KeyExpression { get; } = keyExpression;

    public string? ThreadIsolationLifetime { get; } = threadIsolationLifetime;

    public string? ModuleName { get; } = moduleName;
}
#pragma warning restore S107
