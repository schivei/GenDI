namespace GenDI.SourceGenerator.Models;

#pragma warning disable S107 // model constructors intentionally capture all immutable registration data
internal sealed class ServiceRegistration(
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
    string? moduleName,
    string? directRegistrationStatement = null
)
{
    public string ServiceType { get; } = serviceType;

    public string ImplementationType { get; } = implementationType;

    public string Lifetime { get; } = lifetime;

    public bool AllowMultiple { get; } = allowMultiple;

    public bool UseTryAdd { get; } = useTryAdd;

    public string? ThreadIsolationLifetime { get; } = threadIsolationLifetime;

    public string FactoryBody { get; } = factoryBody;

    public int Order { get; } = order;

    public int Group { get; } = group;

    public string? KeyExpression { get; } = keyExpression;

    public string? EnvironmentName { get; } = environmentName;

    public string? ModuleName { get; } = moduleName;

    public string? DirectRegistrationStatement { get; } = directRegistrationStatement;
}
#pragma warning restore S107
