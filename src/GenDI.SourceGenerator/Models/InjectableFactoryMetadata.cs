using Microsoft.CodeAnalysis;

namespace GenDI.SourceGenerator;

#pragma warning disable S107 // model constructors intentionally capture all immutable registration data
internal sealed class InjectableFactoryMetadata
{
    public InjectableFactoryMetadata(
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
        Lifetime = lifetime;
        ServiceType = serviceType;
        ServiceTypeSymbol = serviceTypeSymbol;
        HasOpenGenericServiceType = hasOpenGenericServiceType;
        Order = order;
        Group = group;
        KeyExpression = keyExpression;
        ThreadIsolationLifetime = threadIsolationLifetime;
        ModuleName = moduleName;
    }

    public string Lifetime { get; }

    public string? ServiceType { get; }

    public ITypeSymbol? ServiceTypeSymbol { get; }

    public bool HasOpenGenericServiceType { get; }

    public int Order { get; }

    public int Group { get; }

    public string? KeyExpression { get; }

    public string? ThreadIsolationLifetime { get; }

    public string? ModuleName { get; }
}
#pragma warning restore S107
