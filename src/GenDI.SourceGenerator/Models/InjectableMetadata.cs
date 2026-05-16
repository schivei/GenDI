using Microsoft.CodeAnalysis;

namespace GenDI.SourceGenerator;

#pragma warning disable S107 // model constructors intentionally capture all immutable registration data
internal sealed class InjectableMetadata
{
    public InjectableMetadata(
        string lifetime,
        string? explicitServiceType,
        ITypeSymbol? explicitServiceTypeSymbol,
        bool hasOpenGenericExplicitServiceType,
        int order,
        int group,
        string? keyExpression,
        string? threadIsolationLifetime,
        string? moduleName
    )
    {
        Lifetime = lifetime;
        ExplicitServiceType = explicitServiceType;
        ExplicitServiceTypeSymbol = explicitServiceTypeSymbol;
        HasOpenGenericExplicitServiceType = hasOpenGenericExplicitServiceType;
        Order = order;
        Group = group;
        KeyExpression = keyExpression;
        ThreadIsolationLifetime = threadIsolationLifetime;
        ModuleName = moduleName;
    }

    public string Lifetime { get; }

    public string? ExplicitServiceType { get; }

    public ITypeSymbol? ExplicitServiceTypeSymbol { get; }

    public bool HasOpenGenericExplicitServiceType { get; }

    public int Order { get; }

    public int Group { get; }

    public string? KeyExpression { get; }

    public string? ThreadIsolationLifetime { get; }

    public string? ModuleName { get; }
}
#pragma warning restore S107
