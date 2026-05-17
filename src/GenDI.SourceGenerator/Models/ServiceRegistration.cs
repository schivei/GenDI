namespace GenDI.SourceGenerator;

#pragma warning disable S107 // model constructors intentionally capture all immutable registration data
internal sealed class ServiceRegistration
{
    public ServiceRegistration(
        string serviceType,
        string implementationType,
        string lifetime,
        bool allowMultiple,
        bool useTryAdd,
        string? threadIsolationLifetime,
        string factoryBody,
        int order,
        int group,
        string? keyExpression,
        string? environmentName,
        string? moduleName
    )
    {
        ServiceType = serviceType;
        ImplementationType = implementationType;
        Lifetime = lifetime;
        AllowMultiple = allowMultiple;
        UseTryAdd = useTryAdd;
        ThreadIsolationLifetime = threadIsolationLifetime;
        FactoryBody = factoryBody;
        Order = order;
        Group = group;
        KeyExpression = keyExpression;
        EnvironmentName = environmentName;
        ModuleName = moduleName;
    }

    public string ServiceType { get; }

    public string ImplementationType { get; }

    public string Lifetime { get; }

    public bool AllowMultiple { get; }

    public bool UseTryAdd { get; }

    public string? ThreadIsolationLifetime { get; }

    public string FactoryBody { get; }

    public int Order { get; }

    public int Group { get; }

    public string? KeyExpression { get; }

    public string? EnvironmentName { get; }

    public string? ModuleName { get; }
}
#pragma warning restore S107
