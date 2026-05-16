using Microsoft.CodeAnalysis;

namespace GenDI.SourceGenerator;

#pragma warning disable S107 // model constructors intentionally capture all immutable registration data
internal sealed class ImplementationCandidate
{
    public ImplementationCandidate(
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
        Symbol = symbol;
        ImplementationType = implementationType;
        Lifetime = lifetime;
        AllowMultiple = allowMultiple;
        UseTryAdd = useTryAdd;
        ThreadIsolationLifetime = threadIsolationLifetime;
        Order = order;
        Group = group;
        ModuleName = moduleName;
    }

    public INamedTypeSymbol Symbol { get; }

    public string ImplementationType { get; }

    public string Lifetime { get; }

    public bool? AllowMultiple { get; }

    public bool? UseTryAdd { get; }

    public string? ThreadIsolationLifetime { get; }

    public int Order { get; }

    public int Group { get; }

    public string? ModuleName { get; }
}
#pragma warning restore S107
