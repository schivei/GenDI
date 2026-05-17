using Microsoft.CodeAnalysis;

namespace GenDI.SourceGenerator.Models;

#pragma warning disable S107 // model constructors intentionally capture all immutable registration data
internal sealed class ImplementationCandidate(
    INamedTypeSymbol symbol,
    string implementationType,
    string lifetime,
    bool? allowMultiple,
    bool? useTryAdd,
    string? threadIsolationLifetime,
    int order,
    int group,
    string? moduleName
)
{
    public INamedTypeSymbol Symbol { get; } = symbol;

    public string ImplementationType { get; } = implementationType;

    public string Lifetime { get; } = lifetime;

    public bool? AllowMultiple { get; } = allowMultiple;

    public bool? UseTryAdd { get; } = useTryAdd;

    public string? ThreadIsolationLifetime { get; } = threadIsolationLifetime;

    public int Order { get; } = order;

    public int Group { get; } = group;

    public string? ModuleName { get; } = moduleName;
}
#pragma warning restore S107
